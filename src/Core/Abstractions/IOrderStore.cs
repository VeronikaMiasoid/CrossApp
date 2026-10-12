using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища замовлень: перелік доступних операцій без жодної згадки про те,
// ЯК саме дані зберігаються (пам'ять, файл чи згодом база даних — тиждень 11).
public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order item);
    void Update(Order item);
    bool Remove(string id);
}
