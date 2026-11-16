namespace Core.Events;

// Додаткове завдання: альтернатива події. Приймач повідомлень передається сервісу через
// конструктор (ручний DI тижня 5). Сам інтерфейс живе в Core, а реалізації, що пишуть у
// консоль чи файл (ConsoleSink, FileSink), — у Cli, бо Core не повинен знати про Console і File.
public interface INotificationSink
{
    void Notify(OrderChangedEventArgs e);
}
