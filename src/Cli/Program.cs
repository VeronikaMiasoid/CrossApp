using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Import;

if (args.Contains("--orders"))
{
    RunOrderDomainDemo();
    return 0;
}

if (args.Contains("--store"))
{
    RunOrderStoreDemo(args.Contains("--file"));
    return 0;
}

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
static void RunOrderDomainDemo()
{
    Console.WriteLine(" Сценарій 1: успіх ");
    var order = Core.Domain.Order.Create("ORD-TEST-1", "CUST-01");
    Console.WriteLine(order);
    order.AddLine("SKU-001", "Цемент М400 25кг", 185.00m, 10);
    order.AddLine("SKU-002", "Пісок будівельний, м3", 420.00m, 2);
    Console.WriteLine(order);
    order.Confirm();
    Console.WriteLine(order);

    Console.WriteLine();
    Console.WriteLine(" Сценарій 2: порушення інваріантів ");

    TryDo("додавання рядка в підтверджене замовлення", () => order.AddLine("SKU-003", "Цегла", 7.50m, 100));
    TryDo("від'ємна кількість у рядку", () => Core.Domain.Order.Create("ORD-TEST-2", "CUST-02").AddLine("SKU-001", "Цемент", 185.00m, -5));
    TryDo("порожній customerId", () => Core.Domain.Order.Create("ORD-TEST-3", "   "));

    var empty = Core.Domain.Order.Create("ORD-TEST-4", "CUST-03");
    TryDo("підтвердження порожнього замовлення", () => empty.Confirm());

    static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}

static void RunOrderStoreDemo(bool useFile)
{
    string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");
    Core.Abstractions.IOrderStore store = useFile
        ? new Core.Storage.FileOrderStore(dataPath)
        : new Core.Storage.InMemoryOrderStore(Core.SampleData.Orders());

    var service = new Core.Services.OrderService(store);
    Console.WriteLine($"Сховище: {store.GetType().Name}");

    var created = service.CreateOrder("CUST-DEMO");
    service.AddLine(created.Id, "SKU-101", "Кабель UTP cat6, м", 14.50m, 50);
    service.AddLine(created.Id, "SKU-102", "Розетка RJ45, шт", 35.00m, 12);
    service.Confirm(created.Id);

    Console.WriteLine();
    Console.WriteLine(" Усі замовлення у сховищі ");
    foreach (var o in service.All())
        Console.WriteLine($"  {o}");

    Console.WriteLine();
    Console.WriteLine(" Сценарій відмови ");
    try
    {
        service.AddLine("NO-SUCH-ID", "SKU-999", "Щось", 1.00m, 1);
        Console.WriteLine("  виняток НЕ спрацював — помилка в логіці сервісу!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {ex.GetType().Name} — {ex.Message}");
    }
}