using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;

namespace Cli;

public static class DomainDemo
{
    public static void Run()
    {
        var d1 = new DateOnly(2026, 10, 1);
        var d2 = new DateOnly(2026, 10, 15);

        Console.WriteLine("=== Сценарій 1: успіх ===");
        var copy = BookCopy.Create("C-001", "9780132350884");
        Console.WriteLine(copy);
        var loan = Loan.Open("L-001", copy, "R-01", d1);
        Console.WriteLine(loan);
        Console.WriteLine(copy);
        loan.Close(d2);
        Console.WriteLine(loan);
        Console.WriteLine(copy);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
        var copy2 = BookCopy.Create("C-002", "9780596007126");
        var loan2 = Loan.Open("L-002", copy2, "R-02", d1);

        TryDo("повторна видача примірника", () => Loan.Open("L-003", copy2, "R-03", d1));
        TryDo("порожній ISBN", () => BookCopy.Create("C-003", " "));
        TryDo("повернення вільного примірника", () => copy.Return());
        TryDo("повернення раніше видачі", () => loan2.Close(d1.AddDays(-1)));
        loan2.Close(d2);
        TryDo("повторне закриття видачі", () => loan2.Close(d2));
        Console.WriteLine($" Після відмов: {copy2}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 3: ToDto / FromDto ===");
        var dto = copy.ToDto();
        Console.WriteLine($" {dto}");
        Console.WriteLine($" Відновлено: {BookCopy.FromDto(dto)}");
        TryDo("FromDto з порожнім ISBN", () => BookCopy.FromDto(dto with { Isbn = "" }));
    }

    public static void RunExtra()
    {
        var d = new DateOnly(2026, 10, 1);

        Console.WriteLine("=== Додатково 1: ImportResult -> сутності ===");
        var imported = new ImportResult<BookDto>(
            [
                new BookDto("B-001", "9780132350884", "Clean Code", 2008, "Robert Martin"),
                new BookDto("B-002", "12345", "Коротка книга", 2020),
                new BookDto("B-003", "9780596007126", "Head First Design Patterns", 2004)
            ],
            ["рядок 7: порожній ISBN"]);
        var converted = BookEntityMapper.ToEntities(imported);
        Console.WriteLine($" Прийнято книг: {converted.Items.Count}, помилок: {converted.Errors.Count}");
        foreach (var error in converted.Errors)
            Console.WriteLine($"  - {error}");

        Console.WriteLine();
        Console.WriteLine("=== Додатково 2: ліміт видач читача ===");
        var service = new LendingService();
        var copies = Enumerable.Range(1, 6)
            .Select(i => BookCopy.Create($"C-{i:00}", "9780132350884")).ToList();
        for (var i = 0; i < 5; i++)
            service.IssueCopy($"L-{i + 1:00}", copies[i], "R-01", d);
        TryDo("6-та видача читачу R-01", () => service.IssueCopy("L-06", copies[5], "R-01", d));
        service.ReturnCopy("L-01", d.AddDays(7));
        TryDo("6-та видача після повернення", () => service.IssueCopy("L-06", copies[5], "R-01", d));

        Console.WriteLine();
        Console.WriteLine("=== Додатково 3: переходи станів ===");
        var c = BookCopy.Create("C-90", "9780596007126");
        var loan = Loan.Open("L-90", c, "R-09", d);
        loan.MarkLost();
        Console.WriteLine($" {loan}");
        TryDo("Lost -> Lost", () => loan.MarkLost());
        loan.Close(d.AddDays(30));
        Console.WriteLine($" {loan}");
        TryDo("Returned -> Lost", () => loan.MarkLost());
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($" {title}: виконано без винятку");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}