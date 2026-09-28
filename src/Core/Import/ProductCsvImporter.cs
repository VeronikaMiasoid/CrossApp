using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах товарів.
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 3 } => new ParseFailed($"очікую 3 колонки, отримав {parts.Length}"),
            ["", _, _] or [_, "", _]
                => new ParseFailed("id або назва порожні"),
            [_, _, var price] when !CsvValues.TryParsePrice(price, out _)
                => new ParseFailed($"ціна '{price}' не є невід'ємним числом (формат 12.50)"),
            [var id, var name, var price]
                => new ParseOk(new ProductDto(id, name, ToPrice(price))),
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    // Викликається лише після того, як when-гілка вже підтвердила, що ціна розбирається.
    private static decimal ToPrice(string text)
    {
        CsvValues.TryParsePrice(text, out decimal price);
        return price;
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
