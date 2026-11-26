using Core.Domain;
using Core.Reports;
using Core.Storage;

public static class ReportsDemo
{
    public static void Run(string cmd)
    {
        var data = SampleData.Report();
        var today = SampleData.ReferenceDate;

        switch (cmd)
        {
            case "report-top":
                foreach (var r in ReportService.TopReaders(data.Loans, 5))
                    Console.WriteLine($"{r.ReaderId,-8} видач: {r.Total,2}  відкритих: {r.Open,2}");
                break;

            case "report-overdue":
                foreach (var o in ReportService.Overdue(data.Loans, today))
                    Console.WriteLine($"{o.LoanId,-6} {o.CopyId,-7} {o.ReaderId,-7} до {o.Due:yyyy-MM-dd}  прострочено на {o.DaysOverdue,3} дн.");
                break;

            case "report-groups":
                foreach (var g in ReportService.BooksByDecade(data.Books))
                    Console.WriteLine($"{g.Label,-10} видань: {g.Count,3}");
                break;

            case "report-sum":
                var s = ReportService.CopyTotals(data.Copies);
                Console.WriteLine($"видань: {s.Books}, примірників: {s.Copies} (виданих {s.Issued}), у середньому {s.AvgPerBook:F2} на видання, максимум {s.MaxPerBook}");
                break;

            case "report-deferred":
                var source = data.Copies.ToList();
                var query = source.Where(c => !c.IsIssued);           // запит лише описано, нічого не виконано
                Console.WriteLine($"Вільних примірників до змін: {query.Count()}");
                source.Add(source.First(c => !c.IsIssued));            // додаємо ще один вільний
                Console.WriteLine($"Вільних примірників після змін: {query.Count()}");
                var snapshot = query.ToList();                         // знімок результату
                source.Clear();
                Console.WriteLine($"У знімку лишилось: {snapshot.Count} (а сам запит після Clear бачить: {query.Count()})");
                break;

            case "report-empty":
                Console.WriteLine($"TopReaders: {ReportService.TopReaders(new List<Loan>(), 5).Count}");
                Console.WriteLine($"Overdue: {ReportService.Overdue(new List<Loan>(), today).Count}");
                Console.WriteLine($"BooksByDecade: {ReportService.BooksByDecade(new List<Book>()).Count}");
                Console.WriteLine($"BookAvailability: {ReportService.BookAvailability(new List<Book>(), new List<BookCopy>()).Count}");
                Console.WriteLine($"PopularTitles: {ReportService.PopularTitles(new List<Loan>(), new List<Book>(), 5).Count}");
                var e = ReportService.CopyTotals(new List<BookCopy>());
                Console.WriteLine($"CopyTotals: видань {e.Books}, примірників {e.Copies}, середнє {e.AvgPerBook:F2}, максимум {e.MaxPerBook}");
                break;

            // ----- додаткове завдання -----
            case "report-join":
                foreach (var b in ReportService.BookAvailability(data.Books, data.Copies))
                    Console.WriteLine($"{b.Title,-34} примірників: {b.Copies,2}  вільних: {b.Free,2}");
                break;

            case "report-popular":
                foreach (var t in ReportService.PopularTitles(data.Loans, data.Books, 5))
                    Console.WriteLine($"{t.Isbn}  {t.Title,-24} видач: {t.Loans,2}");
                break;

            case "report-compare":
                var m = ReportService.BooksByDecade(data.Books);
                var q = ReportVariants.BooksByDecadeQuery(data.Books);
                Console.WriteLine($"груп (методний синтаксис): {m.Count}");
                Console.WriteLine($"груп (запитний синтаксис): {q.Count}");
                Console.WriteLine($"результати однакові: {m.SequenceEqual(q)}");
                break;

            case "report-bench":
                ReportBench.Run();
                break;

            default:
                Console.WriteLine("Невідомий звіт: " + cmd);
                break;
        }
    }
}
