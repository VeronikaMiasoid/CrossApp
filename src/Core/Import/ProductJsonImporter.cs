using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            List<ProductDto?> parsed = JsonSerializer.Deserialize<List<ProductDto?>>(json, Options) ?? [];

            for (int i = 0; i < parsed.Count; i++)
            {
                int number = i + 1;
                ProductDto? p = parsed[i];

                if (p is null)
                    errors.Add($"елемент {number}: порожній об'єкт");
                else if (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name))
                    errors.Add($"елемент {number}: id або назва порожні");
                else if (p.Price < 0)
                    errors.Add($"елемент {number}: ціна {p.Price} від'ємна");
                else
                    items.Add(p);
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"некоректний JSON: {ex.Message}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}
