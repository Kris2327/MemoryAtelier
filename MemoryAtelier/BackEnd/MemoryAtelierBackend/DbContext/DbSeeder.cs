using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Models;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Memory Atelier",
                    Email = "memoryatelier25@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                }
            );
            db.SaveChanges();
        }

        if (!db.Categories.Any())
        {
            var interior = new Category { Id = Guid.NewGuid(), Name = "Интериор", NameEn = "Interior" };
            var exterior = new Category { Id = Guid.NewGuid(), Name = "Екстериор", NameEn = "Exterior" };
            db.Categories.AddRange(interior, exterior);
            db.SaveChanges();

            var decorInt = new Category { Id = Guid.NewGuid(), Name = "Декорации", NameEn = "Decorations", ParentId = interior.Id };
            var candles = new Category { Id = Guid.NewGuid(), Name = "Свещи", NameEn = "Candles", ParentId = interior.Id };
            var services = new Category { Id = Guid.NewGuid(), Name = "Сервизи", NameEn = "Tableware sets", ParentId = interior.Id };
            var holders = new Category { Id = Guid.NewGuid(), Name = "Свещници", NameEn = "Candle holders", ParentId = interior.Id };
            var kashpiInt = new Category { Id = Guid.NewGuid(), Name = "Кашпи", NameEn = "Planters", ParentId = interior.Id };
            var podlozhki = new Category { Id = Guid.NewGuid(), Name = "Подложки", NameEn = "Coasters", ParentId = interior.Id };

            var decorExt = new Category { Id = Guid.NewGuid(), Name = "Декорации", NameEn = "Decorations", ParentId = exterior.Id };
            var saksii = new Category { Id = Guid.NewGuid(), Name = "Саксии", NameEn = "Pots", ParentId = exterior.Id };
            var kashpiExt = new Category { Id = Guid.NewGuid(), Name = "Кашпи", NameEn = "Planters", ParentId = exterior.Id };
            var nastelki = new Category { Id = Guid.NewGuid(), Name = "Настелки", NameEn = "Doormats", ParentId = exterior.Id };

            db.Categories.AddRange(decorInt, candles, services, holders, kashpiInt, podlozhki, decorExt, saksii, kashpiExt, nastelki);
            db.SaveChanges();

            var animals = new Category { Id = Guid.NewGuid(), Name = "Животни", NameEn = "Animals", ParentId = holders.Id };
            var kenichs = new Category { Id = Guid.NewGuid(), Name = "Кенички", NameEn = "Tins", ParentId = holders.Id };
            var kashpiHold = new Category { Id = Guid.NewGuid(), Name = "Кашпи", NameEn = "Planters", ParentId = holders.Id };
            var flowers = new Category { Id = Guid.NewGuid(), Name = "Цветя", NameEn = "Flowers", ParentId = holders.Id };
            db.Categories.AddRange(animals, kenichs, kashpiHold, flowers);
            db.SaveChanges();

            if (!db.Products.Any())
            {
                var candle = new Product { Id = Guid.NewGuid(), Name = "Ароматна свещ", NameEn = "Scented candle", Slug = "scented-candle", Categories = new List<Category> { candles }, Price = 24.00m, Description = "Ръчно лята соева свещ с аромат на сандалово дърво.", DescriptionEn = "Hand-poured soy candle with a sandalwood scent.", Stock = 20, CreatedAt = DateTime.UtcNow };
                candle.Images.Add(new ProductImage { Id = Guid.NewGuid(), ProductId = candle.Id, ImageUrl = "https://images.unsplash.com/photo-1602607526854-61e26948e399?w=600", Order = 0 });

                var vase = new Product { Id = Guid.NewGuid(), Name = "Керамична ваза", NameEn = "Ceramic vase", Slug = "ceramic-vase", Categories = new List<Category> { decorInt }, Price = 48.00m, Description = "Ръчно изработена керамична ваза.", DescriptionEn = "Handmade ceramic vase.", Stock = 8, CreatedAt = DateTime.UtcNow };
                vase.Images.Add(new ProductImage { Id = Guid.NewGuid(), ProductId = vase.Id, ImageUrl = "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?w=600", Order = 0 });

                var pot = new Product { Id = Guid.NewGuid(), Name = "Саксия от теракота", NameEn = "Terracotta pot", Slug = "terracotta-pot", Categories = new List<Category> { saksii }, Price = 32.00m, Description = "Ръчно изработена саксия от теракота.", DescriptionEn = "Handmade terracotta pot.", Stock = 15, CreatedAt = DateTime.UtcNow };
                pot.Images.Add(new ProductImage { Id = Guid.NewGuid(), ProductId = pot.Id, ImageUrl = "https://images.unsplash.com/photo-1595351298020-038700609878?w=600", Order = 0 });

                db.Products.AddRange(candle, vase, pot);
                db.SaveChanges();
            }
        }

        SeedCategoryTranslations(db);
        SeedCategorySortOrder(db);
        SeedProductSlugs(db);
    }

    /// <summary>
    /// Idempotent: assigns a stable initial SortOrder to any sibling group that has never been ordered
    /// (i.e. every sibling still sits at the default 0). Runs on every startup, unlike the main seed block above.
    /// Skips any group that already has an explicit order, so it never overwrites an admin's manual reordering.
    /// </summary>
    private static void SeedCategorySortOrder(AppDbContext db)
    {
        var all = db.Categories.Where(c => !c.IsDeleted).ToList();
        var changed = false;

        foreach (var siblings in all.GroupBy(c => c.ParentId))
        {
            var group = siblings.ToList();
            if (group.Count < 2 || group.Any(c => c.SortOrder != 0)) continue;

            for (var i = 0; i < group.Count; i++)
            {
                group[i].SortOrder = i;
            }
            changed = true;
        }

        if (changed)
        {
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Idempotent: generates a Slug for any existing product left over from before the Slug field was introduced.
    /// Runs on every startup, unlike the main seed block above.
    /// </summary>
    private static void SeedProductSlugs(AppDbContext db)
    {
        var toUpdate = db.Products.IgnoreQueryFilters().Where(p => p.Slug == null || p.Slug == "").ToList();
        if (toUpdate.Count == 0) return;

        var existingSlugs = db.Products.IgnoreQueryFilters()
            .Select(p => p.Slug)
            .Where(s => s != null && s != "")
            .ToHashSet();

        foreach (var product in toUpdate)
        {
            var baseSlug = SlugGenerator.Slugify(product.NameEn ?? product.Name);
            var slug = SlugGenerator.MakeUnique(baseSlug, existingSlugs);
            product.Slug = slug;
            existingSlugs.Add(slug);
        }

        db.SaveChanges();
    }

    /// <summary>
    /// Idempotent: fills in NameEn for any existing category whose name matches a known translation
    /// and doesn't have one yet. Runs on every startup, unlike the main seed block above.
    /// </summary>
    private static void SeedCategoryTranslations(AppDbContext db)
    {
        var translations = new Dictionary<string, string>
        {
            ["Интериор"] = "Interior",
            ["Екстериор"] = "Exterior",
            ["Декорации"] = "Decorations",
            ["Свещи"] = "Candles",
            ["Сервизи"] = "Tableware sets",
            ["Свещници"] = "Candle holders",
            ["Кашпи"] = "Planters",
            ["Подложки"] = "Coasters",
            ["Саксии"] = "Pots",
            ["Настелки"] = "Doormats",
            ["Животни"] = "Animals",
            ["Кенички"] = "Tins",
            ["Цветя"] = "Flowers"
        };

        var toUpdate = db.Categories.Where(c => c.NameEn == null).ToList();
        var changed = false;

        foreach (var category in toUpdate)
        {
            if (translations.TryGetValue(category.Name, out var nameEn))
            {
                category.NameEn = nameEn;
                changed = true;
            }
        }

        if (changed)
        {
            db.SaveChanges();
        }
    }
}