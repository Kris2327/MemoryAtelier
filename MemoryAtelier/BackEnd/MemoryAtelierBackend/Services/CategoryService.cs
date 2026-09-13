using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class CategoryService(AppDbContext db)
{
    public async Task<List<CategoryDto>> GetTreeAsync()
    {
        var all = await db.Categories.ToListAsync();
        return BuildTree(all, null);
    }

    private List<CategoryDto> BuildTree(List<Category> all, Guid? parentId)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.NameEn, c.ParentId, BuildTree(all, c.Id)))
            .ToList();
    }

    public async Task<Category> CreateAsync(CreateCategoryDto dto)
    {
        var maxOrder = await db.Categories
            .Where(c => c.ParentId == dto.ParentId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync() ?? -1;

        var cat = new Category { Id = Guid.NewGuid(), Name = dto.Name, NameEn = dto.NameEn, ParentId = dto.ParentId, SortOrder = maxOrder + 1 };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    public async Task ReorderAsync(ReorderCategoriesDto dto)
    {
        var categories = await db.Categories
            .Where(c => dto.OrderedIds.Contains(c.Id))
            .ToListAsync();

        for (var i = 0; i < dto.OrderedIds.Count; i++)
        {
            var cat = categories.FirstOrDefault(c => c.Id == dto.OrderedIds[i]);
            if (cat != null) cat.SortOrder = i;
        }

        await db.SaveChangesAsync();
    }

    public async Task<(Category? Category, string? Error)> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return (null, null);

        if (dto.ParentId.HasValue && await IsDescendantOrSelfAsync(id, dto.ParentId.Value))
        {
            return (null, "A category cannot become a subcategory of itself or one of its own subcategories.");
        }

        cat.Name = dto.Name;
        cat.NameEn = dto.NameEn;
        cat.ParentId = dto.ParentId;
        await db.SaveChangesAsync();
        return (cat, null);
    }

    private async Task<bool> IsDescendantOrSelfAsync(Guid ancestorId, Guid candidateId)
    {
        Guid? currentId = candidateId;
        while (currentId != null)
        {
            if (currentId == ancestorId) return true;
            currentId = await db.Categories.Where(c => c.Id == currentId).Select(c => c.ParentId).FirstOrDefaultAsync();
        }
        return false;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return false;
        cat.IsDeleted = true;
        cat.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TrashedCategoryDto>> GetDeletedAsync()
    {
        return await db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt)
            .Select(c => new TrashedCategoryDto(c.Id, c.Name, c.NameEn, c.DeletedAt!.Value))
            .ToListAsync();
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (cat == null) return false;
        cat.IsDeleted = false;
        cat.DeletedAt = null;
        await db.SaveChangesAsync();
        return true;
    }
}