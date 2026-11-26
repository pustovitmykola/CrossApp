using System.Diagnostics;
using Core.Domain;
using Core.Reports;
using Core.Storage;

public static class ReportBench
{
    private static int _sink; // щоб результат вимірюваних дій не відкинувся
    private const int Runs = 7;

    public static void Run()
    {
        var today = SampleData.ReferenceDate;
        var loans = Generate(100_000, today);

        int open = 0;
        foreach (var l in loans)
            if (l.Status == LoanStatus.Open) open++;
        int overdue = ReportVariants.OverdueQuery(loans, today).Count();

        Console.WriteLine($"Згенеровано видач: {loans.Count} (відкритих: {open}), прострочених: {overdue}");
        Console.WriteLine();

        double chain = Median(() => ReportVariants.OverdueQuery(loans, today).ToList().Count);
        double middle = Median(() => ReportVariants.OverdueWithMiddleToList(loans, today).Count);
        double twice = Median(() =>
        {
            var q = ReportVariants.OverdueQuery(loans, today);
            int c = q.Count();
            var list = q.ToList();
            return c + list.Count;
        });
        double once = Median(() =>
        {
            var list = ReportVariants.OverdueQuery(loans, today).ToList();
            return list.Count;
        });

        Console.WriteLine($"Ланцюжок без проміжного ToList:             {chain,6:F1} мс");
        Console.WriteLine($"З ToList у середині ланцюжка:               {middle,6:F1} мс");
        Console.WriteLine($"Запит, перебраний двічі (Count+ToList):     {twice,6:F1} мс");
        Console.WriteLine($"Запит, матеріалізований один раз:           {once,6:F1} мс");
    }

    // Один прогрівальний запуск (JIT), далі медіана із семи запусків
    private static double Median(Func<int> action)
    {
        _sink += action();
        var times = new double[Runs];
        for (int i = 0; i < Runs; i++)
        {
            var sw = Stopwatch.StartNew();
            _sink += action();
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times);
        return times[Runs / 2];
    }

    private static List<Loan> Generate(int n, DateOnly today)
    {
        var loans = new List<Loan>(n);
        for (int i = 0; i < n; i++)
        {
            var copy = BookCopy.Create($"BC-{i}", $"978966{i % 1000:D7}");
            var issued = today.AddDays(-(i % 60) - 1);
            var loan = Loan.Open($"BL-{i}", copy, $"R-{i % 500:D3}", issued);
            if (i % 3 == 0) loan.Close(issued.AddDays(5)); // кожна третя видача повернена
            loans.Add(loan);
        }
        return loans;
    }
}
