using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class CartService(AppDbContext db)
{
    public async Task<List<CartItemDto>> GetCartAsync(Guid userId)
    {
        return await db.CartItems
            .Include(c => c.Product).ThenInclude(p => p!.Images)
            .Where(c => c.UserId == userId)
            .Select(c => new CartItemDto(
                c.Id, c.ProductId, c.Product!.Name,
                c.Product.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).FirstOrDefault(),
                c.Product.Price, c.Quantity, c.Product.Price * c.Quantity,
                c.Product.Stock, c.FulfillmentChoice
            )).ToListAsync();
    }

    public async Task<CartItemDto> AddToCartAsync(Guid userId, AddToCartDto dto)
    {
        var existing = await db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);
        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
            // количеството се промени — предишният избор (разделяне/изчакване) вече не е валиден
            existing.FulfillmentChoice = null;
            await db.SaveChangesAsync();
        }
        else
        {
            existing = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                CreatedAt = DateTime.UtcNow
            };
            db.CartItems.Add(existing);
            await db.SaveChangesAsync();
        }
        await db.Entry(existing).Reference(c => c.Product).LoadAsync();
        await db.Entry(existing.Product!).Collection(p => p.Images).LoadAsync();
        return new CartItemDto(existing.Id, existing.ProductId, existing.Product!.Name,
            existing.Product.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).FirstOrDefault(),
            existing.Product.Price, existing.Quantity, existing.Product.Price * existing.Quantity,
            existing.Product.Stock, existing.FulfillmentChoice);
    }

    public async Task<bool> UpdateCartAsync(Guid userId, Guid cartItemId, UpdateCartDto dto)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
        if (item == null) return false;
        item.Quantity = dto.Quantity;
        item.FulfillmentChoice = dto.FulfillmentChoice;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFromCartAsync(Guid userId, Guid cartItemId)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
        if (item == null) return false;
        db.CartItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCartCountAsync(Guid userId) =>
        await db.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);

    public async Task ClearCartAsync(Guid userId)
    {
        var items = await db.CartItems.Where(c => c.UserId == userId).ToListAsync();
        if (items.Count == 0)
        {
            return;
        }

        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync();
    }
}
