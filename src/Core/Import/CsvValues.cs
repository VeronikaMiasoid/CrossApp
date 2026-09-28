using System.Globalization;

namespace Core.Import;

internal static class CsvValues
{
    // Тільки цифри й десяткова крапка. NumberStyles.Number дозволив би роздільник тисяч,
    // і "12,50" перетворилося б на 1250 замість помилки.
    public static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price);
}
