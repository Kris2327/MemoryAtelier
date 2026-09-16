namespace MemoryAtelierBackend.Services;

// Изтрива завинаги продукти и категории, стоящи в кошчето от повече от 30 дни.
public class TrashCleanupService(IServiceProvider services, ILogger<TrashCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

            var cutoff = DateTime.UtcNow - RetentionPeriod;
            var purgedProducts = await productService.PurgeDeletedOlderThanAsync(cutoff);
            var purgedCategories = await categoryService.PurgeDeletedOlderThanAsync(cutoff);

            if (purgedProducts > 0 || purgedCategories > 0)
            {
                logger.LogInformation(
                    "Trash cleanup: purged {Products} product(s) and {Categories} category/-ies older than 30 days.",
                    purgedProducts, purgedCategories);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Trash cleanup run failed.");
        }
    }
}
