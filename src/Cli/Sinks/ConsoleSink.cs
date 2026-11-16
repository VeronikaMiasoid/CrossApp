using Core.Events;

namespace Cli.Sinks;

// Приймач повідомлень, що друкує в консоль. Живе в Cli, бо Core не повинен знати про Console.
public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs e) => Console.WriteLine(EventFormat.Line(e));
}
