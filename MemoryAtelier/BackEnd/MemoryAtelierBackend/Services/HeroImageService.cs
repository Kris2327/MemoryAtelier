using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class HeroImageService(AppDbContext db)
{
    public async Task<List<HeroImageDto>> GetAllAsync()
    {
        return await db.HeroImages
            .OrderBy(h => h.SortOrder)
            .Select(h => new HeroImageDto(h.Id, h.ImageUrl, h.SortOrder))
            .ToListAsync();
    }

    public async Task<HeroImageDto> CreateAsync(CreateHeroImageDto dto)
    {
        var maxOrder = await db.HeroImages.Select(h => (int?)h.SortOrder).MaxAsync() ?? -1;
        var image = new HeroImage
        {
            Id = Guid.NewGuid(),
            ImageUrl = dto.ImageUrl,
            SortOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };
        db.HeroImages.Add(image);
        await db.SaveChangesAsync();
        return new HeroImageDto(image.Id, image.ImageUrl, image.SortOrder);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var image = await db.HeroImages.FirstOrDefaultAsync(h => h.Id == id);
        if (image == null) return false;
        db.HeroImages.Remove(image);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task ReorderAsync(ReorderHeroImagesDto dto)
    {
        var images = await db.HeroImages
            .Where(h => dto.OrderedIds.Contains(h.Id))
            .ToListAsync();

        for (var i = 0; i < dto.OrderedIds.Count; i++)
        {
            var image = images.FirstOrDefault(h => h.Id == dto.OrderedIds[i]);
            if (image != null) image.SortOrder = i;
        }

        await db.SaveChangesAsync();
    }
}
