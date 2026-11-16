using Core.Domain;
using Core.Dto;

namespace Core.Reports;

// Чисті статичні функції: приймають послідовність замовлень, повертають готовий список DTO.
// Ніякого Console, файлів чи стану — тому звіти легко тестувати й повторно використовувати в Api/Blazor.
public static class ReportService
{
    // 1. Where + Select + TopBy: підтверджені замовлення, найбільші за сумою.
    public static IReadOnlyList<OrderSummaryRow> TopConfirmedOrders(IEnumerable<Order> orders, int n = 5)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .Where(o => o.IsConfirmed)
            .Select(o => new OrderSummaryRow(o.Id, o.CustomerId, o.Lines.Count, o.Total))
            .TopBy(r => r.Total, r => r.OrderId, n)
            .ToList();
    }

    // 2. Where з порогом + Select у DTO + OrderByDescending/ThenBy (без TopBy, для порівняння).
    public static IReadOnlyList<OrderSummaryRow> ExpensiveOrders(IEnumerable<Order> orders, decimal threshold)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .Where(o => o.Total > threshold)
            .Select(o => new OrderSummaryRow(o.Id, o.CustomerId, o.Lines.Count, o.Total))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.OrderId, StringComparer.Ordinal)
            .ToList();
    }

    // 3. GroupBy з проєкцією в DTO: статистика по клієнтах.
    public static IReadOnlyList<CustomerGroupRow> ByCustomer(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new CustomerGroupRow(
                g.Key,
                g.Count(),
                g.Sum(o => o.Total),
                g.Max(o => o.Total)))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.CustomerId, StringComparer.Ordinal)
            .ToList();
    }

    // 4. Продажі по товарах: SelectMany розгортає рядки всіх замовлень, потім GroupBy; TopBy обмежує n.
    public static IReadOnlyList<ProductSalesRow> TopProducts(IEnumerable<Order> orders, int n = 5)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .SelectMany(o => o.Lines)
            .GroupBy(l => (l.ProductId, l.ProductName))
            .Select(g => new ProductSalesRow(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(l => l.Quantity),
                g.Sum(l => l.Price * l.Quantity)))
            .TopBy(r => r.Revenue, r => r.ProductId, n)
            .ToList();
    }

    // 5. Агрегати з обробкою порожнього джерела: Sum дає 0, а Average/Max на порожньому кидають виняток.
    public static StatusSummary Summarize(IEnumerable<Order> orders, bool confirmed)
    {
        ArgumentNullException.ThrowIfNull(orders);

        List<decimal> totals = orders
            .Where(o => o.IsConfirmed == confirmed)
            .Select(o => o.Total)
            .ToList();

        if (totals.Count == 0)
            return new StatusSummary(0, 0m, 0m, 0m);

        return new StatusSummary(totals.Count, totals.Sum(), totals.Average(), totals.Max());
    }

    // 6. Join: замовлення + клієнт -> один комбінований DTO (клієнти без замовлень зникають).
    public static IReadOnlyList<(string OrderId, string CustomerName, decimal Total)> OrdersWithCustomer(
        IEnumerable<Order> orders, IEnumerable<CustomerDto> customers)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(customers);

        return orders
            .Join(customers, o => o.CustomerId, c => c.Id,
                (o, c) => (o.Id, c.Name, o.Total))
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
    }

    // 7. GroupJoin: кожен клієнт з його замовленнями (клієнт без замовлень лишається з нулями).
    public static IReadOnlyList<CustomerOrdersRow> CustomersWithOrders(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(orders);

        return customers
            .GroupJoin(orders, c => c.Id, o => o.CustomerId,
                (c, os) => new CustomerOrdersRow(c.Id, c.Name, os.Count(), os.Sum(o => o.Total)))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.CustomerId, StringComparer.Ordinal)
            .ToList();
    }

    // 8. Той самий звіт, що ByCustomer, але в синтаксисі запитів (query syntax) — для порівняння в README.
    public static IReadOnlyList<CustomerGroupRow> ByCustomerQuerySyntax(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        var query =
            from o in orders
            group o by o.CustomerId into g
            orderby g.Sum(x => x.Total) descending, g.Key
            select new CustomerGroupRow(g.Key, g.Count(), g.Sum(x => x.Total), g.Max(x => x.Total));

        return query.ToList();
    }
}
