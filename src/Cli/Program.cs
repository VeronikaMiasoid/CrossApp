using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cli;
using Cli.Sinks;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

bool json = args.Contains("--json");
string? pathArg = args.FirstOrDefault(a => !a.StartsWith("--"));

// Тиждень 7: звіти LINQ. Назва підкоманди — перший аргумент без "--"; --file не вважається назвою звіту.
if (pathArg is not null && pathArg.StartsWith("report-"))
    return ReportRunner.Run(pathArg, args.Contains("--empty"));

// Тиждень 4: доменна модель напряму (успіх + порушення інваріантів).
if (args.Contains("--orders"))
{
    RunOrderDomainDemo();
    return 0;
}

// Тиждень 5: сервіс + сховище (пам'ять або файл).
if (args.Contains("--store"))
{
    RunOrderStoreDemo(args.Contains("--file"));
    return 0;
}

// Тиждень 6: подія Changed, два підписники, відписка, відмови.
if (args.Contains("--events"))
{
    RunEventsDemo();
    return 0;
}

// Тиждень 6 (додаткове): INotificationSink через конструктор замість події.
if (args.Contains("--sink"))
{
    RunSinkDemo();
    return 0;
}

// Тиждень 6 (додаткове): Action<string> як параметр методу імпорту.
if (args.Contains("--skipped"))
    return RunSkippedDemo(pathArg ?? Path.Combine("data", "sample.csv"));

if (args.Contains("--env"))
{
    PrintEnvironment(EnvironmentInfo.Collect(), json);
    return 0;
}

if (args.Contains("--mixed"))
    return RunMixed(pathArg ?? Path.Combine("data", "mixed.csv"));

return RunImport(pathArg ?? Path.Combine("data", "sample.csv"));

// ---------------------------------------------------------------- тиждень 4

static void RunOrderDomainDemo()
{
    Console.WriteLine("Сценарій 1: успіх");
    var order = Order.Create("ORD-TEST-1", "CUST-01");
    Console.WriteLine(order);
    order.AddLine("SKU-001", "Цемент М400 25кг", 185.00m, 10);
    order.AddLine("SKU-002", "Пісок будівельний, м3", 420.00m, 2);
    Console.WriteLine(order);
    order.Confirm();
    Console.WriteLine(order);

    Console.WriteLine();
    Console.WriteLine("Сценарій 2: порушення інваріантів");

    TryDo("додавання рядка в підтверджене замовлення", () => order.AddLine("SKU-003", "Цегла", 7.50m, 100));
    TryDo("від'ємна кількість у рядку", () => Order.Create("ORD-TEST-2", "CUST-02").AddLine("SKU-001", "Цемент", 185.00m, -5));
    TryDo("порожній customerId", () => Order.Create("ORD-TEST-3", "   "));

    var empty = Order.Create("ORD-TEST-4", "CUST-03");
    TryDo("підтвердження порожнього замовлення", () => empty.Confirm());
}

// Спільний обробник для сценаріїв відмови: перехоплює виняток і друкує лише Message, без stack trace.
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

// ---------------------------------------------------------------- тиждень 5

static void RunOrderStoreDemo(bool useFile)
{
    string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");
    IOrderStore store = useFile
        ? new FileOrderStore(dataPath)
        : new InMemoryOrderStore(SampleData.Orders());

    var service = new OrderService(store);
    Console.WriteLine($"Сховище: {store.GetType().Name}");

    var created = service.CreateOrder("CUST-DEMO");
    service.AddLine(created.Id, "SKU-101", "Кабель UTP cat6, м", 14.50m, 50);
    service.AddLine(created.Id, "SKU-102", "Розетка RJ45, шт", 35.00m, 12);
    service.Confirm(created.Id);

    Console.WriteLine();
    Console.WriteLine("Усі замовлення у сховищі");
    foreach (var o in service.All())
        Console.WriteLine($"  {o}");

    Console.WriteLine();
    Console.WriteLine("Сценарій відмови");
    TryDo("додати рядок у неіснуюче замовлення", () => service.AddLine("NO-SUCH-ID", "SKU-999", "Щось", 1.00m, 1));
}

// ---------------------------------------------------------------- тиждень 6

static void RunEventsDemo()
{
    // Журнал живе в каталозі збірки; каталог створює код (інакше DirectoryNotFoundException).
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    File.Delete(logPath); // свіжий журнал для демонстрації; якщо файла немає — виняток не кидається

    var service = new OrderService(new InMemoryOrderStore(SampleData.Orders()));

    // Підписник 1: ЛОКАЛЬНИЙ МЕТОД. Його можна передати і в +=, і в -= (одне й те саме ім'я).
    // Виводить рядок у консоль і дописує його у файл; помилка файлу не ламає бізнес-операцію.
    void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        string line = EventFormat.Line(e);
        Console.WriteLine(line);
        try
        {
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"  ! журнал недоступний: {ex.Message}");
        }
    }

    service.Changed += OnOrderChanged;

    // Підписник 2: ЛЯМБДА із замиканням над локальною змінною changes.
    // (_, _) — два відкидання: параметри sender і e потрібні сигнатурі, але тіло їх не читає.
    int changes = 0;
    service.Changed += (_, _) => changes++;

    Console.WriteLine("Сценарій 1: зміни замовлення (обидва підписники працюють)");
    var order = service.CreateOrder("CUST-EVENTS");
    service.AddLine(order.Id, "SKU-101", "Кабель UTP cat6, м", 14.50m, 50);
    service.AddLine(order.Id, "SKU-102", "Розетка RJ45, шт", 35.00m, 12);
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("Сценарій 2: відмови (рядків про зміну бути не повинно)");
    TryDo("замовлення з неіснуючим id", () => service.AddLine("NO-SUCH-ID", "SKU-999", "Скоба", 1.00m, 1));
    TryDo("від'ємна кількість у рядку", () => service.AddLine(order.Id, "SKU-103", "Скоба", 1.00m, -5));
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("Сценарій 3: після відписки");
    service.Changed -= OnOrderChanged;
    service.Confirm(order.Id); // тиха операція для консолі й файлу: підписку знято
    Console.WriteLine($"Подій отримано: {changes}"); // +1 лише від лямбди-лічильника

    Console.WriteLine();
    Console.WriteLine("Сценарій 4: Func замість події (критерій пошуку задає викликач)");
    foreach (var o in service.FindAll(o => o.Total > 5000m).Take(5))
        Console.WriteLine($"  {o}");

    Console.WriteLine();
    Console.WriteLine($"Журнал: {logPath}");
}

static void RunSinkDemo()
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    File.Delete(logPath);

    // Cli (composition root) вирішує, які приймачі підставити, і передає їх сервісу через конструктор.
    INotificationSink sink = new CompositeSink(new ConsoleSink(), new FileSink(logPath));
    var service = new OrderService(new InMemoryOrderStore(), sink);

    Console.WriteLine("INotificationSink: приймач передано сервісу через конструктор");
    var order = service.CreateOrder("CUST-SINK");
    service.AddLine(order.Id, "SKU-201", "Патч-корд 1 м", 22.00m, 10);
    service.AddLine(order.Id, "SKU-202", "Патч-корд 3 м", 41.50m, 4);
    service.Confirm(order.Id);

    Console.WriteLine($"Журнал: {logPath}");
}

static int RunSkippedDemo(string path)
{
    if (!FileExists(path))
        return 1;

    if (Path.GetExtension(path).ToLowerInvariant() != ".csv")
    {
        Console.WriteLine("Звіт про пропущені рядки через Action<string> підтримує лише .csv");
        return 2;
    }

    // Лямбда із замиканням над лічильником skipped: імпортер викликає її для кожного пропущеного рядка.
    int skipped = 0;
    Action<string> onSkipped = message =>
    {
        skipped++;
        Console.WriteLine($" [пропущено #{skipped}] {message}");
    };

    ImportResult<ProductDto> result = ProductCsvImporter.Load(path, onSkipped);

    Console.WriteLine($"Прийнято: {result.Items.Count}, повідомлень через Action: {skipped}, помилок у результаті: {result.Errors.Count}");
    return 0;
}

// ---------------------------------------------------------------- тижні 1-3

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
