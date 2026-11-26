using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public const int LoanPeriodDays = 14; // як у LendingService: дата видачі + 14 днів

    // Топ-N читачів: групування + TopBy (стабільне сортування, другий ключ ReaderId)
    public static IReadOnlyList<ReaderLoanCount> TopReaders(IEnumerable<Loan> loans, int n) =>
        loans.GroupBy(l => l.ReaderId)
             .Select(g => new ReaderLoanCount(g.Key, g.Count(), g.Count(l => l.Status == LoanStatus.Open)))
             .TopBy(r => r.Total, n, r => r.ReaderId)
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

    // ДОДАТКОВО: GroupJoin (як LEFT JOIN) - видання без примірників не зникають, а лишаються з нулями
    public static IReadOnlyList<BookAvailabilityLine> BookAvailability(
        IEnumerable<Book> books, IEnumerable<BookCopy> copies) =>
        books.GroupJoin(copies,
                        b => b.Isbn,
                        c => c.Isbn,
                        (b, cs) => new BookAvailabilityLine(b.Title, cs.Count(), cs.Count(c => !c.IsIssued)))
             .OrderBy(x => x.Free)
             .ThenBy(x => x.Title, StringComparer.Ordinal)
             .ToList();

    // ДОДАТКОВО: Join (як INNER JOIN) - видачі групуються за ISBN і з'єднуються з виданнями, далі топ-N через TopBy
    public static IReadOnlyList<TitleLoanCount> PopularTitles(
        IEnumerable<Loan> loans, IEnumerable<Book> books, int n) =>
        loans.GroupBy(l => l.Copy.Isbn)
             .Select(g => new { Isbn = g.Key, Count = g.Count() })
             .Join(books,
                   x => x.Isbn,
                   b => b.Isbn,
                   (x, b) => new TitleLoanCount(b.Isbn, b.Title, x.Count))
             .TopBy(t => t.Loans, n, t => t.Isbn)
             .ToList();
}
