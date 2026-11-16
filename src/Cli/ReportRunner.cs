using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Dto;
using Core.Reports;

namespace Cli;

// Друк звітів тижня 7. Увесь LINQ живе тут і в Core.Reports; Program.cs лише передає назву підкоманди.
internal static class ReportRunner
{
    public static int Run(string name, bool empty)
    {
        // --empty підміняє джерело порожньою послідовністю: звіти не мають падати.
        IEnumerable<Order> orders = empty ? Enumerable.Empty<Order>() : SampleData.Orders();
        List<CustomerDto> customers = SampleData.Customers();

        switch (name)
        {
            case "report-top":
                Print("Топ-5 підтверджених замовлень за сумою",
                    ReportService.TopConfirmedOrders(orders, 5),
                    r => $" {r.OrderId,-9} {r.CustomerId,-5} рядків {r.LinesCount} сума {r.Total,10:0.00}");
                return 0;

            case "report-top-orders":
                Print("Топ-5 товарів за виручкою",
                    ReportService.TopProducts(orders, 5),
                    r => $" {r.ProductId,-8} {r.ProductName,-26} к-сть {r.Quantity,5} виручка {r.Revenue,10:0.00}");
                return 0;

            case "report-expensive":
                Print("Замовлення дорожчі за 5000",
                    ReportService.ExpensiveOrders(orders, 5000m),
                    r => $" {r.OrderId,-9} {r.CustomerId,-5} сума {r.Total,10:0.00}");
                return 0;

            case "report-groups":
                Print("Статистика по клієнтах (GroupBy)",
                    ReportService.ByCustomer(orders),
                    r => $" {r.CustomerId,-5} замовлень {r.OrdersCount} разом {r.Total,10:0.00} найбільше {r.MaxOrder,10:0.00}");
                return 0;

            case "report-sum":
                PrintSummary("Підтверджені", ReportService.Summarize(orders, true));
                PrintSummary("Чернетки   ", ReportService.Summarize(orders, false));
                return 0;

            case "report-join":
                Print("Join: замовлення з іменем клієнта",
                    ReportService.OrdersWithCustomer(orders, customers),
                    r => $" {r.OrderId,-9} {r.CustomerName,-22} {r.Total,10:0.00}");
                Print("GroupJoin: клієнти з їхніми замовленнями",
                    ReportService.CustomersWithOrders(customers, orders),
                    r => $" {r.CustomerId,-5} {r.CustomerName,-22} замовлень {r.OrdersCount} разом {r.Total,10:0.00}");
                return 0;

            case "report-query":
                RunQuerySyntax(orders);
                return 0;

            case "report-lazy":
                RunLazy();
                return 0;

            case "report-bench":
                RunBench();
                return 0;

            default:
                Console.WriteLine($"Невідомий звіт '{name}'. Доступні: report-top, report-top-orders, report-expensive, " +
                                  "report-groups, report-sum, report-join, report-query, report-lazy, report-bench");
                return 2;
        }
    }

    private static void Print<T>(string title, IReadOnlyList<T> rows, Func<T, string> format)
    {
        Console.WriteLine(title);
        if (rows.Count == 0)
        {
            Console.WriteLine(" (порожньо)");
            return;
        }

        foreach (T row in rows)
            Console.WriteLine(format(row));
    }

    private static void PrintSummary(string label, StatusSummary s) =>
        Console.WriteLine($" {label} кількість {s.Count,3} сума {s.Sum,10:0.00} середнє {s.Average,9:0.00} максимум {s.Max,9:0.00}");

    // Той самий результат двома синтаксисами: SequenceEqual порівнює записи за значенням (record).
    private static void RunQuerySyntax(IEnumerable<Order> orders)
    {
        var method = ReportService.ByCustomer(orders);
        var query = ReportService.ByCustomerQuerySyntax(orders);

        Print("Запит у query syntax (from ... group ... into ... orderby ... select)", query,
            r => $" {r.CustomerId,-5} замовлень {r.OrdersCount} разом {r.Total,10:0.00}");
        Console.WriteLine($"Збігається з method syntax: {method.SequenceEqual(query)}");
    }

    // Відкладене виконання: запит будується один раз, але виконується при кожному переборі.
    private static void RunLazy()
    {
        var source = new List<Order>(SampleData.Orders());

        // Запит ЩЕ НЕ виконується: тут лише описано, що робити.
        IEnumerable<Order> confirmed = source.Where(o => o.IsConfirmed);
        Console.WriteLine($"Запит побудовано. Замовлень у джерелі: {source.Count}");

        int first = confirmed.Count();                       // виконання №1
        Console.WriteLine($"Count() до зміни джерела    : {first}");

        source.Add(NewConfirmed("ORD-X001"));                // змінюємо джерело ПІСЛЯ побудови запиту
        int second = confirmed.Count();                      // виконання №2 бачить новий елемент
        Console.WriteLine($"Count() після зміни джерела : {second}");

        // ToList() «знімає знімок»: подальші зміни джерела на нього не впливають.
        List<Order> snapshot = confirmed.ToList();
        source.Add(NewConfirmed("ORD-X002"));
        Console.WriteLine($"Знімок ToList()              : {snapshot.Count} (властивість Count, без повторного виконання)");
        Console.WriteLine($"Запит confirmed.Count() знову: {confirmed.Count()}");
    }

    private static Order NewConfirmed(string id)
    {
        var order = Order.Create(id, "C-01");
        order.AddLine("SKU-001", "Цемент М400 25кг", 185.00m, 1);
        order.Confirm();
        return order;
    }

    // Стенд: 100 000 згенерованих замовлень; ToList у середині ланцюжка проти суцільного ланцюжка.
    private static void RunBench()
    {
        List<Order> big = Generate(100_000);
        Console.WriteLine($"Згенеровано замовлень: {big.Count}");

        const int Runs = 5;
        long withToList = Measure(Runs, () =>
            big.Where(o => o.IsConfirmed).ToList()
               .Select(o => o.Total).ToList()
               .Where(t => t > 1000m).Sum());

        long streaming = Measure(Runs, () =>
            big.Where(o => o.IsConfirmed)
               .Select(o => o.Total)
               .Where(t => t > 1000m).Sum());

        Console.WriteLine($"З ToList() у середині : {withToList} мс (середнє з {Runs})");
        Console.WriteLine($"Суцільний ланцюжок    : {streaming} мс (середнє з {Runs})");
    }

    private static long Measure(int runs, Func<decimal> action)
    {
        action(); // прогрів JIT, у вимірі не враховується
        var sw = Stopwatch.StartNew();
        decimal sink = 0;
        for (int i = 0; i < runs; i++)
            sink += action();
        sw.Stop();
        GC.KeepAlive(sink);
        return sw.ElapsedMilliseconds / runs;
    }

    private static List<Order> Generate(int count)
    {
        var rnd = new Random(42); // фіксоване зерно: однакові дані між запусками
        var list = new List<Order>(count);
        for (int i = 0; i < count; i++)
        {
            var order = Order.Create($"G-{i:000000}", $"C-{rnd.Next(1, 11):00}");
            order.AddLine("SKU-001", "Товар", rnd.Next(10, 500), rnd.Next(1, 20));
            if (rnd.Next(2) == 0)
                order.Confirm();
            list.Add(order);
        }
        return list;
    }
}
