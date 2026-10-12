using Core.Domain;

namespace Core;

// Фіксований набір замовлень для демонстрацій (тиждень 5, режим "у пам'яті")
// і для майбутніх звітів (тиждень 7).
public static class SampleData
{
    public static List<Order> Orders()
    {
        var list = new List<Order>
        {
            Build("ORD-0001", "C-01", confirm: true,
                ("SKU-001", "Цемент М400 25кг", 185.00m, 10),
                ("SKU-002", "Пісок будівельний, м3", 420.00m, 2)),

            Build("ORD-0002", "C-02", confirm: true,
                ("SKU-003", "Цегла червона, шт", 7.50m, 500)),

            Build("ORD-0003", "C-01", confirm: false,
                ("SKU-004", "Арматура 10мм, м", 32.00m, 60)),

            Build("ORD-0004", "C-03", confirm: true,
                ("SKU-005", "Профнастил С8, м2", 210.00m, 25),
                ("SKU-006", "Саморізи покрівельні, уп", 95.00m, 4)),

            Build("ORD-0005", "C-04", confirm: true,
                ("SKU-001", "Цемент М400 25кг", 185.00m, 20)),

            Build("ORD-0006", "C-05", confirm: false,
                ("SKU-007", "Утеплювач мінвата, м2", 145.00m, 40),
                ("SKU-002", "Пісок будівельний, м3", 420.00m, 1)),

            Build("ORD-0007", "C-02", confirm: true,
                ("SKU-008", "Фарба фасадна, 10л", 980.00m, 3)),

            Build("ORD-0008", "C-06", confirm: true,
                ("SKU-003", "Цегла червона, шт", 7.50m, 1200),
                ("SKU-004", "Арматура 10мм, м", 32.00m, 80)),

            Build("ORD-0009", "C-07", confirm: false,
                ("SKU-009", "Ламінат клас 32, м2", 310.00m, 55)),

            Build("ORD-0010", "C-01", confirm: true,
                ("SKU-010", "Герметик силіконовий, шт", 120.00m, 12)),

            Build("ORD-0011", "C-08", confirm: true,
                ("SKU-005", "Профнастил С8, м2", 210.00m, 18),
                ("SKU-006", "Саморізи покрівельні, уп", 95.00m, 3)),

            Build("ORD-0012", "C-03", confirm: false,
                ("SKU-001", "Цемент М400 25кг", 185.00m, 15)),

            Build("ORD-0013", "C-09", confirm: true,
                ("SKU-007", "Утеплювач мінвата, м2", 145.00m, 60)),

            Build("ORD-0014", "C-04", confirm: true,
                ("SKU-008", "Фарба фасадна, 10л", 980.00m, 2),
                ("SKU-010", "Герметик силіконовий, шт", 120.00m, 6)),

            Build("ORD-0015", "C-10", confirm: false,
                ("SKU-009", "Ламінат клас 32, м2", 310.00m, 30)),

            Build("ORD-0016", "C-05", confirm: true,
                ("SKU-002", "Пісок будівельний, м3", 420.00m, 3)),
        };

        return list;
    }

    private static Order Build(string id, string customerId, bool confirm,
        params (string ProductId, string Name, decimal Price, int Qty)[] lines)
    {
        var order = Order.Create(id, customerId);

        foreach (var (productId, name, price, qty) in lines)
            order.AddLine(productId, name, price, qty);

        if (confirm)
            order.Confirm();

        return order;
    }
}
