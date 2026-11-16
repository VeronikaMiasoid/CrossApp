namespace Core.Reports;

public static class EnumerableExtensions
{
    // Власний розширювальний метод: n найбільших елементів за ключем.
    // Другий ключ tieBreaker (ThenBy з Ordinal) робить порядок детермінованим при рівних значеннях.
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> key,
        Func<T, string> tieBreaker,
        int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        return source
            .OrderByDescending(key)
            .ThenBy(tieBreaker, StringComparer.Ordinal)
            .Take(n);
    }
}
