using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class FavouritesService(AppDbContext db)
{
    public async Task<List<FavouriteDto>> GetFavouritesAsync(Guid userId)
    {
        return await db.Favourites
            .Include(f => f.Product).ThenInclude(p => p!.Images)
            .Where(f => f.UserId == userId)
            .Select(f => new FavouriteDto(
                f.Id, f.ProductId, f.Product!.Name,
                f.Product.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).FirstOrDefault(),
                f.Product.Price
            )).ToListAsync();
    }

    public async Task<bool> ToggleFavouriteAsync(Guid userId, Guid productId)
    {
        var existing = await db.Favourites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        if (existing != null)
        {
            db.Favourites.Remove(existing);
            await db.SaveChangesAsync();
            return false;
        }
        db.Favourites.Add(new Favourite { Id = Guid.NewGuid(), UserId = userId, ProductId = productId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetFavouritesCountAsync(Guid userId) =>
        await db.Favourites.CountAsync(f => f.UserId == userId);
}