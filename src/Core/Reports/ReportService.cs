using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public const int LoanPeriodDays = 14; // як у LendingService: дата видачі + 14 днів

    // Топ-N читачів: групування + стабільне сортування (другий ключ ReaderId)
    public static IReadOnlyList<ReaderLoanCount> TopReaders(IEnumerable<Loan> loans, int n) =>
        loans.GroupBy(l => l.ReaderId)
             .Select(g => new ReaderLoanCount(g.Key, g.Count(), g.Count(l => l.Status == LoanStatus.Open)))
             .OrderByDescending(r => r.Total)
             .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
             .Take(n)
             .ToList();

    // Прострочені: фільтр -> проєкція в DTO -> фільтр -> сортування; today передається параметром
    public static IReadOnlyList<OverdueLine> Overdue(IEnumerable<Loan> loans, DateOnly today) =>
        loans.Where(l => l.Status == LoanStatus.Open)
             .Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(LoanPeriodDays) })
             .Where(x => x.Due < today)
             .Select(x => new OverdueLine(x.Loan.Id, x.Loan.Copy.Id, x.Loan.ReaderId,
                                          x.Due, today.DayNumber - x.Due.DayNumber))
             .OrderByDescending(o => o.DaysOverdue)
             .ThenBy(o => o.LoanId, StringComparer.Ordinal)
             .ToList();

    // Групування за десятиліттям: ключ = рік / 10 * 10 (значення, а не посилання)
    public static IReadOnlyList<GroupSummary> BooksByDecade(IEnumerable<Book> books) =>
        books.GroupBy(b => b.Year / 10 * 10)
             .OrderBy(g => g.Key)
             .Select(g => new GroupSummary($"{g.Key}-ті", g.Count()))
             .ToList();

    // Агрегати з обробкою порожньої послідовності (Average/Max на порожній кидають виняток)
    public static CopyStats CopyTotals(IEnumerable<BookCopy> copies)
    {
        var list = copies.ToList();
        if (list.Count == 0) return new CopyStats(0, 0, 0, 0.0, 0);

        var perBook = list.GroupBy(c => c.Isbn).Select(g => g.Count()).ToList();
        return new CopyStats(perBook.Count, list.Count, list.Count(c => c.IsIssued),
                             perBook.Average(), perBook.Max());
    }
}
