namespace Core.Domain;

// Один рядок замовлення: товар + кількість + ціна, зафіксована на момент додавання.
// Це значення (value object) всередині агрегату Order: власного фабричного методу й
// інваріантів не має, бо всі перевірки виконує Order.AddLine ДО того, як рядок створюється.
public sealed record OrderLine(string ProductId, string ProductName, decimal Price, int Quantity)
{
    public decimal LineTotal => Price * Quantity;
}
