namespace Core.Events;

// Вид зміни замовлення. Перелік оголошено в домені: підписник отримує не "магічне число",
// а ім'я зі скінченного набору. Числовий код першої константи (OrderCreated) дорівнює 0.
public enum OrderChangeKind { OrderCreated, LineAdded, OrderConfirmed }
