using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

// Різнорідні рядки в одному файлі: "P;..." — товар, "C;..." — клієнт.
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductRow p:
                    products.Add(p.Value);
                    break;
                case CustomerRow c:
                    customers.Add(c.Value);
                    break;
                case RowFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static RowOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var name, var price]
                when id != "" && name != "" && CsvValues.TryParsePrice(price, out _)
                => new ProductRow(new ProductDto(id, name, decimal.Parse(
                    price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))),
            ["P", ..]
                => new RowFailed("товар: очікую P;id;назва;ціна (id і назва не порожні, ціна на кшталт 12.50)"),
            ["C", var id, var name] when id != "" && name != ""
                => new CustomerRow(new CustomerDto(id, name)),
            ["C", var id, var name, var email] when id != "" && name != ""
                => new CustomerRow(new CustomerDto(id, name, email == "" ? null : email)),
            ["C", ..]
                => new RowFailed("клієнт: очікую C;id;ім'я[;email] (id та ім'я не порожні)"),
            [var kind, ..]
                => new RowFailed($"невідомий тип рядка '{kind}'"),
            _ => new RowFailed("порожній рядок")
        };
    }

    private abstract record RowOutcome;
    private sealed record ProductRow(ProductDto Value) : RowOutcome;
    private sealed record CustomerRow(CustomerDto Value) : RowOutcome;
    private sealed record RowFailed(string Reason) : RowOutcome;
}
