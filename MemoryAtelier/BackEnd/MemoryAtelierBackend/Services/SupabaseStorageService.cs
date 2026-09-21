using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MemoryAtelierBackend.DTOs;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MemoryAtelierBackend.Services;

public class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseSettings> settingsOptions,
    ILogger<SupabaseStorageService> logger)
{
    private readonly SupabaseSettings _settings = settingsOptions.Value;

    // Снимките от клиенти (телефони и т.н.) често са по няколко MB — това взриви LCP-то на сайта
    // (виж PageSpeed: 60+ MB картинки на начална страница). Преоразмеряваме и компресираме преди upload.
    private const int MaxDimension = 1920;
    // Малка "-thumb" версия за решетки/миниатюри — Supabase-ият image-transformation endpoint (render/image)
    // се оказа недостъпен за проекта (403), затова генерираме статична миниатюра сами при качване.
    private const int ThumbnailMaxDimension = 480;
    private const int WebpQuality = 80;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ProjectUrl) &&
        !string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) &&
        !string.IsNullOrWhiteSpace(_settings.StorageBucket);

    public async Task<string> UploadImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Supabase storage is not configured.");
        }

        await EnsureBucketExistsAsync(cancellationToken);

        var (mainStream, thumbStream, contentType, extension) = await PrepareImageAsync(file, cancellationToken);
        using (mainStream)
        using (thumbStream)
        {
            var objectPath = $"products/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";
            await UploadObjectAsync(_settings.StorageBucket, objectPath, mainStream, contentType, upsert: false, cancellationToken);

            if (thumbStream != null)
            {
                try
                {
                    await UploadObjectAsync(_settings.StorageBucket, ToThumbnailPath(objectPath), thumbStream, contentType, upsert: false, cancellationToken);
                }
                catch (Exception ex)
                {
                    // не чупим целия upload заради миниатюрата — фронтендът пада обратно на основната снимка при 404
                    logger.LogWarning(ex, "Thumbnail upload failed for {ObjectPath}, continuing without it.", objectPath);
                }
            }

            return $"{_settings.ProjectUrl.TrimEnd('/')}/storage/v1/object/public/{_settings.StorageBucket}/{objectPath}";
        }
    }

    // GIF-овете (може да са анимирани) се качват непроменени, без миниатюра; всичко останало се
    // преоразмерява до MaxDimension и се пренакодира в WebP, плюс отделна ThumbnailMaxDimension "-thumb" версия.
    private static async Task<(Stream MainStream, Stream? ThumbStream, string ContentType, string Extension)> PrepareImageAsync(
        IFormFile file, CancellationToken cancellationToken)
    {
        if (file.ContentType == "image/gif")
        {
            return (file.OpenReadStream(), null, file.ContentType, Path.GetExtension(file.FileName));
        }

        await using var source = file.OpenReadStream();
        using var image = await Image.LoadAsync(source, cancellationToken);

        using var thumbImage = image.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension)
        }));
        var thumbOutput = new MemoryStream();
        await thumbImage.SaveAsync(thumbOutput, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
        thumbOutput.Position = 0;

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaxDimension, MaxDimension)
        }));

        var output = new MemoryStream();
        await image.SaveAsync(output, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
        output.Position = 0;
        return (output, thumbOutput, "image/webp", ".webp");
    }

    // Преоразмерява "на място" стари снимки, качени преди MaxDimension/WebP логиката по-горе — тегли текущия
    // обект, смалява го ако е над MaxDimension и го качва обратно на СЪЩИЯ path (x-upsert), така че URL-ът в
    // базата да не се променя. Освен това (винаги, дори когато основната снимка вече е ОК) генерира и качва
    // липсващата "-thumb" companion версия, използвана от фронтенда за решетки/миниатюри.
    public async Task<ImageReprocessResult> ReprocessIfOversizedAsync(string publicImageUrl, CancellationToken cancellationToken = default)
    {
        const string marker = "/storage/v1/object/public/";
        var idx = publicImageUrl.IndexOf(marker, StringComparison.Ordinal);
        if (idx == -1) return ImageReprocessResult.Skipped;

        var rest = publicImageUrl[(idx + marker.Length)..];
        var slashIdx = rest.IndexOf('/');
        if (slashIdx == -1) return ImageReprocessResult.Skipped;
        var bucket = rest[..slashIdx];
        var objectPath = rest[(slashIdx + 1)..];

        using var getResponse = await httpClient.GetAsync(publicImageUrl, cancellationToken);
        if (!getResponse.IsSuccessStatusCode) return ImageReprocessResult.Failed;

        var originalBytes = await getResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = getResponse.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        Image image;
        try
        {
            using var sourceStream = new MemoryStream(originalBytes);
            image = await Image.LoadAsync(sourceStream, cancellationToken);
        }
        catch
        {
            return ImageReprocessResult.Failed;
        }

        using (image)
        {
            var wasOversized = image.Width > MaxDimension || image.Height > MaxDimension;
            var format = image.Metadata.DecodedImageFormat;

            try
            {
                using var thumbImage = image.Clone(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension)
                }));
                using var thumbOutput = new MemoryStream();
                if (format != null) await thumbImage.SaveAsync(thumbOutput, format, cancellationToken);
                else await thumbImage.SaveAsync(thumbOutput, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
                thumbOutput.Position = 0;
                await UploadObjectAsync(bucket, ToThumbnailPath(objectPath), thumbOutput, contentType, upsert: true, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Thumbnail generation/upload failed for {ObjectPath}", objectPath);
            }

            if (!wasOversized)
            {
                return ImageReprocessResult.AlreadyOptimized;
            }

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxDimension, MaxDimension)
            }));

            using var output = new MemoryStream();
            if (format != null) await image.SaveAsync(output, format, cancellationToken);
            else await image.SaveAsync(output, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
            output.Position = 0;

            try
            {
                await UploadObjectAsync(bucket, objectPath, output, contentType, upsert: true, cancellationToken);
            }
            catch
            {
                return ImageReprocessResult.Failed;
            }

            return ImageReprocessResult.Reprocessed;
        }
    }

    private static string ToThumbnailPath(string objectPath)
    {
        var dot = objectPath.LastIndexOf('.');
        return dot == -1 ? objectPath + "-thumb" : objectPath[..dot] + "-thumb" + objectPath[dot..];
    }

    private async Task UploadObjectAsync(string bucket, string objectPath, Stream content, string contentType, bool upsert, CancellationToken cancellationToken)
    {
        var uploadUrl = $"{_settings.ProjectUrl.TrimEnd('/')}/storage/v1/object/{bucket}/{objectPath}";

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = streamContent };
        AddAuthHeaders(request.Headers);
        request.Headers.TryAddWithoutValidation("x-upsert", upsert ? "true" : "false");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Supabase storage upload failed for {ObjectPath} with status {StatusCode}: {Body}", objectPath, response.StatusCode, body);
            throw new InvalidOperationException("Image upload to Supabase Storage failed.");
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketUrl = $"{_settings.ProjectUrl.TrimEnd('/')}/storage/v1/bucket";
        using var request = new HttpRequestMessage(HttpMethod.Post, bucketUrl);
        AddAuthHeaders(request.Headers);

        var payload = new
        {
            id = _settings.StorageBucket,
            name = _settings.StorageBucket,
            @public = true,
            allowed_mime_types = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Supabase bucket ensure failed with status {StatusCode}: {Body}", response.StatusCode, body);
    }

    private void AddAuthHeaders(HttpRequestHeaders headers)
    {
        headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ServiceRoleKey);
        headers.TryAddWithoutValidation("apikey", _settings.ServiceRoleKey);
    }
}

public enum ImageReprocessResult { Reprocessed, AlreadyOptimized, Skipped, Failed }
