using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;

namespace MemoryAtelierBackend.Services;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var totalProducts = await db.Products.CountAsync();
        var totalCategories = await db.Categories.CountAsync();
        var totalOrders = await db.Orders.CountAsync();
        var totalRevenue = await db.Orders.SumAsync(order => (decimal?)order.TotalAmount) ?? 0m;
        var newUsersThisWeek = await db.Users.CountAsync(user => user.CreatedAt >= weekAgo);

        var favouritesData = await db.Favourites
            .Include(f => f.Product)
            .ToListAsync();

        var mostFavourited = favouritesData
            .GroupBy(f => new { f.ProductId, ProductName = f.Product != null ? f.Product.Name : "Unknown product" })
            .Select(group => new MostFavouritedProductDto(group.Key.ProductName, group.Count()))
            .OrderByDescending(item => item.FavouriteCount)
            .ThenBy(item => item.Name)
            .FirstOrDefault();

        var lowStockProducts = await db.Products
            .Include(product => product.Categories)
            .Include(product => product.Images)
            .Where(product => product.Stock < 5)
            .OrderBy(product => product.Stock)
            .ThenBy(product => product.Name)
            .Select(product => new LowStockProductDto(
                product.Id,
                product.Name,
                product.NameEn,
                product.Categories.Select(c => new CategoryRefDto(c.Id, c.Name, c.NameEn, c.IsHidden)).ToList(),
                product.Stock,
                product.Images.OrderBy(image => image.Order).Select(image => image.ImageUrl).FirstOrDefault()
            ))
            .ToListAsync();

        return new DashboardStatsDto(
            totalProducts,
            totalCategories,
            totalOrders,
            totalRevenue,
            newUsersThisWeek,
            mostFavourited,
            lowStockProducts
        );
    }

    public async Task<DashboardChartsDto> GetChartsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var revenueMonths = Enumerable.Range(0, 6)
            .Select(offset => monthStart.AddMonths(-(5 - offset)))
            .ToList();

        var revenueRaw = await db.Orders
            .Where(order => order.CreatedAt >= revenueMonths.First())
            .ToListAsync();

        var revenueByMonth = revenueMonths
            .Select(month => new RevenueByMonthDto(
                month.ToString("MMM"),
                revenueRaw
                    .Where(order => order.CreatedAt.Year == month.Year && order.CreatedAt.Month == month.Month)
                    .Sum(order => order.TotalAmount)
            ))
            .ToList();

        var orderItemsData = await db.OrderItems
            .Include(item => item.Product)
            .ThenInclude(product => product!.Categories)
            .ToListAsync();

        var salesByCategory = orderItemsData
            .SelectMany(item =>
            {
                var categoryNames = item.Product != null && item.Product.Categories.Count > 0
                    ? item.Product.Categories.Select(c => c.Name)
                    : new[] { "No category" };
                return categoryNames.Select(name => new { Category = name, item.Quantity });
            })
            .GroupBy(x => x.Category)
            .Select(group => new SalesByCategoryDto(group.Key, group.Sum(x => x.Quantity)))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Category)
            .ToList();

        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var weekBuckets = Enumerable.Range(0, 6)
            .Select(offset => weekStart.AddDays(-7 * (5 - offset)))
            .ToList();

        var usersRaw = await db.Users
            .Where(user => user.CreatedAt >= weekBuckets.First())
            .ToListAsync();

        var newUsersByWeek = weekBuckets
            .Select((start, index) =>
            {
                var end = start.AddDays(7);
                var count = usersRaw.Count(user => user.CreatedAt >= start && user.CreatedAt < end);
                return new NewUsersByWeekDto($"Week {index + 1}", count);
            })
            .ToList();

        return new DashboardChartsDto(revenueByMonth, salesByCategory, newUsersByWeek);
    }
}
