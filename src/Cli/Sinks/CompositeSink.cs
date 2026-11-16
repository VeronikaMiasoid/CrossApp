using Core.Events;

namespace Cli.Sinks;

// Об'єднує кілька приймачів в один: сервіс отримує один INotificationSink,
// а повідомлення йде і в консоль, і у файл. Приймачі викликаються по черзі.
public sealed class CompositeSink(params INotificationSink[] sinks) : INotificationSink
{
    public void Notify(OrderChangedEventArgs e)
    {
        foreach (var sink in sinks)
            sink.Notify(e);
    }
}
