namespace Core.Dto;

// Формат зберігання рядка замовлення (DTO-стиль тижня 3): просто дані, без поведінки.
public sealed record OrderLineDto(string ProductId, string ProductName, decimal Price, int Quantity);

// Формат зберігання замовлення: сюди серіалізується Order.ToDto(), звідси відновлюється
// Order.FromDto(). DTO не містить жодної перевірки — за коректність відповідає сутність.
public sealed record OrderDto(string Id, string CustomerId, bool IsConfirmed, IReadOnlyList<OrderLineDto> Lines);
