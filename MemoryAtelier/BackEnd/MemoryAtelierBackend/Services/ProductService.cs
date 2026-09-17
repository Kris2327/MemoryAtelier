using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class ProductService(AppDbContext db)
{
    public async Task<List<ProductDto>> GetAllAsync(Guid? categoryId = null, bool isAdmin = false)
    {
        var query = db.Products.Include(p => p.Categories).Include(p => p.Images).AsQueryable();
        if (categoryId.HasValue)
        {
            var ids = await GetCategoryAndChildIds(categoryId.Value);
            query = query.Where(p => p.Categories.Any(c => ids.Contains(c.Id)));
        }

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        if (!isAdmin)
        {
            products = products.Where(IsVisibleToPublic).ToList();
        }

        return products.Select(p => ToDto(p, isAdmin)).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, bool isAdmin = false)
    {
        var p = await db.Products.Include(p => p.Categories).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        return p == null ? null : ToDto(p, isAdmin);
    }

    // Продукт се вижда публично, ако не е скрит сам по себе си и има поне една видима категория (или изобщо няма категории).
    private static bool IsVisibleToPublic(Product p) =>
        !p.IsHidden && (p.Categories.Count == 0 || p.Categories.Any(c => !c.IsHidden));

    public async Task<ProductDto?> SetHiddenAsync(Guid id, bool hidden)
    {
        var product = await db.Products.Include(p => p.Categories).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return null;

        product.IsHidden = hidden;
        await db.SaveChangesAsync();
        return ToDto(product, isAdmin: true);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var categories = await db.Categories.Where(c => dto.CategoryIds.Contains(c.Id)).ToListAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            NameEn = dto.NameEn,
            Slug = await GenerateUniqueSlugAsync(dto.NameEn ?? dto.Name),
            Categories = categories,
            Price = dto.Price,
            Description = dto.Description,
            DescriptionEn = dto.DescriptionEn,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 0; i < dto.ImageUrls.Count; i++)
        {
            product.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = dto.ImageUrls[i],
                Order = i
            });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await db.Products.Include(p => p.Categories).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return null;

        var nameChanged = product.Name != dto.Name || product.NameEn != dto.NameEn;

        product.Name = dto.Name;
        product.NameEn = dto.NameEn;
        if (nameChanged)
        {
            product.Slug = await GenerateUniqueSlugAsync(dto.NameEn ?? dto.Name, excludeProductId: product.Id);
        }
        product.Price = dto.Price;
        product.Description = dto.Description;
        product.DescriptionEn = dto.DescriptionEn;
        product.Stock = dto.Stock;

        var categories = await db.Categories.Where(c => dto.CategoryIds.Contains(c.Id)).ToListAsync();
        product.Categories.Clear();
        foreach (var category in categories)
        {
            product.Categories.Add(category);
        }

        var oldImages = product.Images.ToList();
        db.ProductImages.RemoveRange(oldImages);
        product.Images.Clear();

        for (int i = 0; i < dto.ImageUrls.Count; i++)
        {
            db.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = dto.ImageUrls[i],
                Order = i
            });
        }

        await db.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return false;
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TrashedProductDto>> GetDeletedAsync()
    {
        return await db.Products
            .IgnoreQueryFilters()
            .Include(p => p.Images)
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt)
            .Select(p => new TrashedProductDto(
                p.Id, p.Name, p.NameEn,
                p.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).FirstOrDefault(),
                p.Price, p.DeletedAt!.Value
            ))
            .ToListAsync();
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);
        if (product == null) return false;
        product.IsDeleted = false;
        product.DeletedAt = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PurgeResult> PurgeAsync(Guid id)
    {
        var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);
        if (product == null) return PurgeResult.NotFound;

        if (await db.OrderItems.AnyAsync(oi => oi.ProductId == id))
        {
            return PurgeResult.HasReferences;
        }

        db.CartItems.RemoveRange(await db.CartItems.Where(c => c.ProductId == id).ToListAsync());
        db.Favourites.RemoveRange(await db.Favourites.Where(f => f.ProductId == id).ToListAsync());
        db.Reviews.RemoveRange(await db.Reviews.Where(r => r.ProductId == id).ToListAsync());
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return PurgeResult.Purged;
    }

    public async Task<int> PurgeDeletedOlderThanAsync(DateTime cutoffUtc)
    {
        var ids = await db.Products.IgnoreQueryFilters()
            .Where(p => p.IsDeleted && p.DeletedAt < cutoffUtc)
            .Select(p => p.Id)
            .ToListAsync();

        var purged = 0;
        foreach (var id in ids)
        {
            if (await PurgeAsync(id) == PurgeResult.Purged) purged++;
        }
        return purged;
    }

    public async Task<ProductDto?> UpdateStockAsync(Guid id, int stock)
    {
        var product = await db.Products
            .Include(p => p.Categories)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        product.Stock = stock;
        await db.SaveChangesAsync();

        return ToDto(product);
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeProductId = null)
    {
        var baseSlug = SlugGenerator.Slugify(name);
        var existingSlugs = await db.Products
            .IgnoreQueryFilters()
            .Where(p => excludeProductId == null || p.Id != excludeProductId)
            .Select(p => p.Slug)
            .ToListAsync();

        return SlugGenerator.MakeUnique(baseSlug, existingSlugs.ToHashSet());
    }

    private async Task<List<Guid>> GetCategoryAndChildIds(Guid categoryId)
    {
        var all = await db.Categories.ToListAsync();
        var result = new List<Guid>();
        CollectIds(all, categoryId, result);
        return result;
    }

    private void CollectIds(List<Category> all, Guid id, List<Guid> result)
    {
        result.Add(id);
        foreach (var child in all.Where(c => c.ParentId == id))
            CollectIds(all, child.Id, result);
    }

    private static ProductDto ToDto(Product p, bool isAdmin = true) => new(
        p.Id, p.Name, p.NameEn, p.Slug,
        p.Categories
            .Where(c => isAdmin || !c.IsHidden)
            .Select(c => new CategoryRefDto(c.Id, c.Name, c.NameEn, c.IsHidden))
            .ToList(),
        p.Price, p.Description, p.DescriptionEn,
        p.Images.OrderBy(i => i.Order).Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.Order)).ToList(),
        p.Stock, p.CreatedAt, p.IsHidden
    );
}
