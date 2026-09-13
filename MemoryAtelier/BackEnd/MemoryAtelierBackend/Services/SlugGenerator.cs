using System.Text;

namespace MemoryAtelierBackend.Services;

public static class SlugGenerator
{
    // Стандартна българска транслитерация (както в паспорти/пътни знаци) — за четими URL slug-ове от кирилски имена
    private static readonly Dictionary<char, string> Transliteration = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y",
        ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh",
        ['щ'] = "sht", ['ъ'] = "a", ['ь'] = "", ['ю'] = "yu", ['я'] = "ya"
    };

    public static string Slugify(string text)
    {
        var builder = new StringBuilder();

        foreach (var ch in text.ToLowerInvariant())
        {
            if (Transliteration.TryGetValue(ch, out var replacement))
            {
                builder.Append(replacement);
            }
            else if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "product" : slug;
    }

    /// <summary>Генерира уникален slug, добавяйки -2, -3... при колизия с вече заетите slug-ове.</summary>
    public static string MakeUnique(string baseSlug, ISet<string> existingSlugs)
    {
        if (!existingSlugs.Contains(baseSlug)) return baseSlug;

        var counter = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        } while (existingSlugs.Contains(candidate));

        return candidate;
    }
}
