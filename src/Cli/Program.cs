using System.Runtime.InteropServices;
using System.Text.Json;

bool jsonMode = args.Contains("--json");

var info = new EnvironmentInfo
{
    Title = "CrossApp – практикум з крос-платформного програмування",
    Student = "Мʼясоїд Вероніка, група ФЕІ-31",
    OsDescription = RuntimeInformation.OSDescription,
    OsEnvironment = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotnetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (Customer, Product, Order, OrderLine) — оформлення замовлень і підрахунок сум"
};

if (jsonMode)
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine(info.Title);
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)   : {info.OsDescription}");
    Console.WriteLine($"ОС (Environment)     : {info.OsEnvironment}");
    Console.WriteLine($"Архітектура процесу  : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)    : {info.DotnetVersion}");
    Console.WriteLine($"Runtime              : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку   : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог     : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}

// Модель для виводу у JSON (System.Text.Json серіалізує публічні властивості)
class EnvironmentInfo
{
    public string Title { get; set; } = "";
    public string Student { get; set; } = "";
    public string OsDescription { get; set; } = "";
    public string OsEnvironment { get; set; } = "";
    public string ProcessArchitecture { get; set; } = "";
    public string DotnetVersion { get; set; } = "";
    public string Runtime { get; set; } = "";
    public string AppDirectory { get; set; } = "";
    public string CurrentDirectory { get; set; } = "";
    public string Domain { get; set; } = "";
}
