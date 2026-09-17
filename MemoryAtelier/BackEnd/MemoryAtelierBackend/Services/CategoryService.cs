using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class CategoryService(AppDbContext db)
{
    public async Task<List<CategoryDto>> GetTreeAsync(bool includeHidden)
    {
        var all = await db.Categories.ToListAsync();
        return BuildTree(all, null, includeHidden);
    }

    private List<CategoryDto> BuildTree(List<Category> all, Guid? parentId, bool includeHidden)
    {
        return all
            .Where(c => c.ParentId == parentId && (includeHidden || !c.IsHidden))
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.NameEn, c.ParentId, BuildTree(all, c.Id, includeHidden), c.IsHidden))
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

    // Изтрива категорията и цялото ѝ активно поддърво (децата вървят заедно с родителя в кошчето).
    public async Task<bool> DeleteAsync(Guid id)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return false;

        var idsToDelete = new List<Guid> { id };
        idsToDelete.AddRange(await CollectActiveDescendantIdsAsync(id));

        var now = DateTime.UtcNow;
        var categories = await db.Categories.Where(c => idsToDelete.Contains(c.Id)).ToListAsync();
        foreach (var category in categories)
        {
            category.IsDeleted = true;
            category.DeletedAt = now;
        }

        await db.SaveChangesAsync();
        return true;
    }

    // Връща изтритите категории като дърво: корените са категориите, чийто родител не е (вече) в кошчето;
    // техните изтрити подкатегории се показват вложени под тях, така както са изглеждали преди изтриването.
    public async Task<List<TrashedCategoryDto>> GetDeletedAsync()
    {
        var all = await db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync();

        var deletedIds = all.Select(c => c.Id).ToHashSet();
        var roots = all.Where(c => c.ParentId == null || !deletedIds.Contains(c.ParentId.Value));

        return roots.Select(c => BuildTrashNode(c, all)).ToList();
    }

    private static TrashedCategoryDto BuildTrashNode(Category cat, List<Category> allDeleted)
    {
        var children = allDeleted
            .Where(c => c.ParentId == cat.Id)
            .Select(c => BuildTrashNode(c, allDeleted))
            .ToList();
        return new TrashedCategoryDto(cat.Id, cat.Name, cat.NameEn, cat.DeletedAt!.Value, children);
    }

    // Скрива категорията и цялото ѝ активно поддърво от публичния сайт (без да ги трие).
    public async Task<bool> HideAsync(Guid id)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return false;

        var idsToHide = new List<Guid> { id };
        idsToHide.AddRange(await CollectActiveDescendantIdsAsync(id));

        var categories = await db.Categories.Where(c => idsToHide.Contains(c.Id)).ToListAsync();
        foreach (var category in categories)
        {
            category.IsHidden = true;
        }

        await db.SaveChangesAsync();
        return true;
    }

    // Показва обратно категорията заедно с цялото ѝ активно поддърво.
    public async Task<bool> ShowAsync(Guid id)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return false;

        var idsToShow = new List<Guid> { id };
        idsToShow.AddRange(await CollectActiveDescendantIdsAsync(id));

        var categories = await db.Categories.Where(c => idsToShow.Contains(c.Id)).ToListAsync();
        foreach (var category in categories)
        {
            category.IsHidden = false;
        }

        await db.SaveChangesAsync();
        return true;
    }

    // Възстановява категорията заедно с цялото ѝ поддърво, останало в кошчето — иначе възстановените деца
    // биха увиснали под все още изтрит родител и изчезват от активното дърво.
    public async Task<bool> RestoreAsync(Guid id)
    {
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (cat == null) return false;

        var idsToRestore = new List<Guid> { id };
        idsToRestore.AddRange(await CollectDeletedDescendantIdsAsync(id));

        var categories = await db.Categories.IgnoreQueryFilters().Where(c => idsToRestore.Contains(c.Id)).ToListAsync();
        foreach (var category in categories)
        {
            category.IsDeleted = false;
            category.DeletedAt = null;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PurgeResult> PurgeAsync(Guid id)
    {
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (cat == null) return PurgeResult.NotFound;

        var descendants = await CollectAllDescendantsAsync(id);
        if (descendants.Any(c => !c.IsDeleted))
        {
            return PurgeResult.HasReferences;
        }

        // Изтрива поддървото отдолу нагоре (децата преди родителя), защото Category->Parent е с DeleteBehavior.Restrict.
        var group = new List<Category> { cat }.Concat(descendants).ToList();
        while (group.Count > 0)
        {
            var leaves = group.Where(c => !group.Any(other => other.ParentId == c.Id)).ToList();
            db.Categories.RemoveRange(leaves);
            await db.SaveChangesAsync();
            group = group.Except(leaves).ToList();
        }

        return PurgeResult.Purged;
    }

    public async Task<int> PurgeDeletedOlderThanAsync(DateTime cutoffUtc)
    {
        var ids = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.IsDeleted && c.DeletedAt < cutoffUtc)
            .Select(c => c.Id)
            .ToListAsync();

        var purged = 0;
        foreach (var id in ids)
        {
            if (await PurgeAsync(id) == PurgeResult.Purged) purged++;
        }
        return purged;
    }

    private async Task<List<Guid>> CollectActiveDescendantIdsAsync(Guid rootId)
    {
        var all = await db.Categories.ToListAsync();
        var result = new List<Guid>();
        void Collect(Guid parentId)
        {
            foreach (var child in all.Where(c => c.ParentId == parentId))
            {
                result.Add(child.Id);
                Collect(child.Id);
            }
        }
        Collect(rootId);
        return result;
    }

    private async Task<List<Guid>> CollectDeletedDescendantIdsAsync(Guid rootId)
    {
        var allDeleted = await db.Categories.IgnoreQueryFilters().Where(c => c.IsDeleted).ToListAsync();
        var result = new List<Guid>();
        void Collect(Guid parentId)
        {
            foreach (var child in allDeleted.Where(c => c.ParentId == parentId))
            {
                result.Add(child.Id);
                Collect(child.Id);
            }
        }
        Collect(rootId);
        return result;
    }

    private async Task<List<Category>> CollectAllDescendantsAsync(Guid rootId)
    {
        var all = await db.Categories.IgnoreQueryFilters().ToListAsync();
        var result = new List<Category>();
        void Collect(Guid parentId)
        {
            foreach (var child in all.Where(c => c.ParentId == parentId))
            {
                result.Add(child);
                Collect(child.Id);
            }
        }
        Collect(rootId);
        return result;
    }
}