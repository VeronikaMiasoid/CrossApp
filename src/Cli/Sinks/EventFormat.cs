using Core.Events;

namespace Cli.Sinks;

// Єдиний формат рядка про подію: його використовують і підписник у Program.cs, і приймачі.
// {e.At:HH:mm:ss}            — час (години:хвилини:секунди)
// {e.Kind,-14}               — вирівнювання вліво шириною 14 символів
// {e.TotalDelta,9:+0.00;-0.00} — дві секції формату: перша для додатних (з "+"), друга для від'ємних (з "-")
internal static class EventFormat
{
    public static string Line(OrderChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} {e.TotalDelta,9:+0.00;-0.00} → рядків {e.LinesCount}, сума {e.Total:0.00}";
}
