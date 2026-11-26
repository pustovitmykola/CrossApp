namespace Core.Reports;

public static class EnumerableExtensions
{
    /// <summary>
    /// Перші n елементів за числовим ключем (за спаданням); нічию за потреби розв'язує другий ключ.
    /// Аргументи перевіряються одразу, а сам запит лишається відкладеним.
    /// </summary>
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, int> key,
        int n,
        Func<T, string>? thenBy = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), n, "N не може бути від'ємним");

        IOrderedEnumerable<T> ordered = source.OrderByDescending(key);
        if (thenBy is not null)
            ordered = ordered.ThenBy(thenBy, StringComparer.Ordinal);

        return ordered.Take(n);
    }
}
