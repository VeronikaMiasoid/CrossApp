namespace Core.Events;

// Дані події: що саме сталося із замовленням. Усі значення описують стан ПІСЛЯ зміни.
// Клас успадковує EventArgs (базовий тип даних події в .NET) і додає поля конкретної зміни.
public sealed class OrderChangedEventArgs(
    string orderId, OrderChangeKind kind, int linesCount, decimal total, decimal totalDelta) : EventArgs
{
    public string OrderId { get; } = orderId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;      // кількість рядків після зміни
    public decimal Total { get; } = total;            // сума замовлення після зміни
    public decimal TotalDelta { get; } = totalDelta;  // на скільки змінилась сума

    // Момент створення аргументів фіксується один раз, а не обчислюється при кожному зверненні.
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
