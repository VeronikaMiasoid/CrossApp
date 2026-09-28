using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Import;

bool json = args.Contains("--json");
string? pathArg = args.FirstOrDefault(a => !a.StartsWith("--"));

if (args.Contains("--env"))
{
    PrintEnvironment(EnvironmentInfo.Collect(), json);
    return 0;
}

if (args.Contains("--mixed"))
    return RunMixed(pathArg ?? Path.Combine("data", "mixed.csv"));

return RunImport(pathArg ?? Path.Combine("data", "sample.csv"));

static int RunImport(string path)
{
    if (!FileExists(path))
        return 1;

    Func<string, ImportResult<ProductDto>>? importer = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load,
        ".json" => ProductJsonImporter.Load,
        _ => null
    };

    if (importer is null)
    {
        Console.WriteLine($"Непідтримуваний формат '{Path.GetExtension(path)}': очікую .csv або .json");
        return 2;
    }

    ImportResult<ProductDto> result = importer(path);

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($" {p.Id,-6} {p.Name,-28} {FormatPrice(p.Price),10}");

    PrintErrors(result.Errors);
    Console.WriteLine(result.Summary);
    return 0;
}

static int RunMixed(string path)
{
    if (!FileExists(path))
        return 1;

    MixedImportResult result = MixedCsvImporter.Load(path);

    Console.WriteLine($"Товарів: {result.Products.Count}, клієнтів: {result.Customers.Count}");
    foreach (ProductDto p in result.Products)
        Console.WriteLine($" [товар]  {p.Id,-6} {p.Name,-28} {FormatPrice(p.Price),10}");
    foreach (CustomerDto c in result.Customers)
        Console.WriteLine($" [клієнт] {c.Id,-6} {c.Name,-28} {c.Email ?? "—"}");

    PrintErrors(result.Errors);
    return 0;
}

static bool FileExists(string path)
{
    if (File.Exists(path))
        return true;

    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return false;
}

static string FormatPrice(decimal price) => price.ToString("F2", CultureInfo.InvariantCulture);

static void PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
        return;

    Console.WriteLine($"Пропущено рядків: {errors.Count}");
    foreach (string e in errors)
        Console.WriteLine($" ! {e}");
}

static void PrintEnvironment(EnvironmentReport report, bool json)
{
    if (json)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        Console.WriteLine(JsonSerializer.Serialize(report, options));
        return;
    }

    Console.WriteLine("CrossApp – інформація про середовище");
    Console.WriteLine("Студент: Мʼясоїд Вероніка, група ФЕІ-31");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)   : {report.OsVersion}");
    Console.WriteLine($"Runtime            : {report.FrameworkDescription}");
    Console.WriteLine($"Версія CLR         : {report.ClrVersion}");
    Console.WriteLine($"Архітектура        : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено)    : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)     : {report.ReportedRid}");
    Console.WriteLine($"Збірка             : {report.BuildNote}");
    Console.WriteLine($"Каталог застосунку : {report.BaseDirectory}");
    Console.WriteLine($"Поточний каталог   : {report.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення (Customer, Product, Order, OrderLine)");
}
