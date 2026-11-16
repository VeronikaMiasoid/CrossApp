using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Сервіс не знає слів "File", "Console" і "List<Order>": лише бізнес-операції поверх
// абстракції сховища та подія Changed для зовнішнього коду.
public sealed class OrderService(IOrderStore store, INotificationSink? sink = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    // Подія: зовнішній код може лише підписатися (+=) чи відписатися (-=);
    // викликати її може тільки цей клас.
    public event EventHandler<OrderChangedEventArgs>? Changed;

    // Єдине місце підняття події. Викликається ЛИШЕ після успішної зміни стану:
    // якщо інваріант кинув виняток, до цього рядка виконання не доходить.
    private void OnChanged(Order order, OrderChangeKind kind, decimal totalDelta)
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total, totalDelta);
        Changed?.Invoke(this, args);   // ?. — якщо підписників немає, Changed дорівнює null
        _sink?.Notify(args);           // додатковий механізм (INotificationSink), якщо його передали
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated, 0m);   // після зміни, не до
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");

        decimal before = order.Total;
        order.AddLine(productId, productName, price, quantity);   // якщо верхній рядок кине виняток — події не буде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded, order.Total - before);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");

        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed, 0m);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string orderId) => _store.GetById(orderId);

    // Поруч із Find(id) з тижня 5: тут критерій пошуку задає той, хто викликає.
    // Func (а не подія), бо потрібна відповідь: "так" чи "ні" для кожного замовлення.
    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}
