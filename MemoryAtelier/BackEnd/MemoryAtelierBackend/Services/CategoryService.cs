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
            .Select(c => new CategoryDto(c.Id, c.Name, c.NameEn, c.ParentId, BuildTree(all, c.Id)))
            .ToList();
    }

    public async Task<Category> CreateAsync(CreateCategoryDto dto)
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = dto.Name, NameEn = dto.NameEn, ParentId = dto.ParentId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    public async Task<Category?> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return null;

        cat.Name = dto.Name;
        cat.NameEn = dto.NameEn;
        await db.SaveChangesAsync();
        return cat;
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