using Core.Dto;

namespace Core.Domain;

// Замовлення — сутність-агрегат: Order є "коренем", OrderLine існує лише в межах свого
// замовлення і ззовні напряму не змінюється.
//
// Інваріанти (реалізовані нижче):
//   1. Id та CustomerId не порожні — перевіряється у Create.
//   2. Кількість у рядку > 0, ціна рядка >= 0 — перевіряється у AddLine.
//   3. Не можна додати рядок до вже підтвердженого замовлення — перевіряється у AddLine.
//   4. Не можна підтвердити замовлення без жодного рядка — перевіряється у Confirm.
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public bool IsConfirmed { get; private set; }

    // Назовні віддаємо лише представлення для читання: зовнішній код не зможе
    // додати чи прибрати рядок в обхід AddLine/Confirm.
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    // Єдиний спосіб створити замовлення: усі перевірки тут, до виклику приватного конструктора.
    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string productName, decimal price, int quantity)
    {
        if (IsConfirmed)
            throw new InvalidOperationException(
                $"Замовлення {Id} вже підтверджене, рядки додавати не можна");
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару не може бути порожнім", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Кількість у рядку має бути більшою за нуль");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                "Ціна рядка не може бути від'ємною");

        _lines.Add(new OrderLine(productId.Trim(), productName.Trim(), price, quantity));
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException(
                $"Замовлення {Id} не можна підтвердити: немає жодного рядка");

        IsConfirmed = true;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5 (FileOrderStore).
    public OrderDto ToDto() => new(
        Id,
        CustomerId,
        IsConfirmed,
        _lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.Price, l.Quantity)).ToList());

    // FromDto проходить ТІ САМІ перевірки, що й під час першого створення: Create + AddLine
    // для кожного рядка, і лише потім (за потреби) Confirm. Пошкоджений файл не зможе
    // відновити некоректне замовлення в обхід інваріантів.
    public static Order FromDto(OrderDto dto)
    {
        var order = Create(dto.Id, dto.CustomerId);

        foreach (var line in dto.Lines)
            order.AddLine(line.ProductId, line.ProductName, line.Price, line.Quantity);

        if (dto.IsConfirmed)
            order.Confirm();

        return order;
    }

    public override string ToString() =>
        $"{Id} [{CustomerId}] {(IsConfirmed ? "підтверджене" : "чернетка")} — {_lines.Count} рядк.(ів), сума {Total:0.00}";
}
