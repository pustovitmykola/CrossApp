using Core.Domain;

namespace Core.Reports;

public static class ReportVariants
{
    // ДОДАТКОВО: той самий звіт у синтаксисі запитів (from ... group ... by ... into ... orderby ... select)
    public static IReadOnlyList<GroupSummary> BooksByDecadeQuery(IEnumerable<Book> books) =>
        (from b in books
         group b by b.Year / 10 * 10 into g
         orderby g.Key
         select new GroupSummary($"{g.Key}-ті", g.Count())).ToList();

    // Для вимірювань: прострочені видачі БЕЗ завершального оператора (відкладений запит)
    public static IEnumerable<OverdueLine> OverdueQuery(IEnumerable<Loan> loans, DateOnly today) =>
        loans.Where(l => l.Status == LoanStatus.Open)
             .Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(ReportService.LoanPeriodDays) })
             .Where(x => x.Due < today)
             .Select(x => new OverdueLine(x.Loan.Id, x.Loan.Copy.Id, x.Loan.ReaderId,
                                          x.Due, today.DayNumber - x.Due.DayNumber))
             .OrderByDescending(o => o.DaysOverdue)
             .ThenBy(o => o.LoanId, StringComparer.Ordinal);

    // Для вимірювань: той самий звіт, але з проміжним ToList посередині ланцюжка
    public static IReadOnlyList<OverdueLine> OverdueWithMiddleToList(IEnumerable<Loan> loans, DateOnly today)
    {
        var open = loans.Where(l => l.Status == LoanStatus.Open).ToList();
        return open.Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(ReportService.LoanPeriodDays) })
                   .Where(x => x.Due < today)
                   .Select(x => new OverdueLine(x.Loan.Id, x.Loan.Copy.Id, x.Loan.ReaderId,
                                                x.Due, today.DayNumber - x.Due.DayNumber))
                   .OrderByDescending(o => o.DaysOverdue)
                   .ThenBy(o => o.LoanId, StringComparer.Ordinal)
                   .ToList();
    }
}
