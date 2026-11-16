using Core.Events;

namespace Cli.Sinks;

// Приймач повідомлень, що дописує рядок у файл журналу. Каталог створюється кодом.
public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public void Notify(OrderChangedEventArgs e) =>
        File.AppendAllText(_path, EventFormat.Line(e) + Environment.NewLine);
}
