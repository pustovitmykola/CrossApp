using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class LendingDemo
{
    public static void Run(string[] args)
    {
        // ===== COMPOSITION ROOT: єдине місце, де вибираємо конкретне сховище =====
        IBookCopyStore store = args.Contains("--file")
            ? new FileBookCopyStore(Path.Combine("data", "catalog.json"))
            : new InMemoryBookCopyStore(SampleData.Copies());

        var service = new LendingService(store);
        // =========================================================================

        Console.WriteLine($"Сховище: {store.GetType().Name}");
        Console.WriteLine($"Примірників на старті: {store.List().Count}");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var isbn = "9780132350884";

        // --- Сценарій 1: успіх ---
        Console.WriteLine("\n== Сценарій 1: успіх ==");
        var added = service.AddBook(isbn);
        Console.WriteLine($"Додано примірник {added.Id}. Тепер у сховищі: {store.List().Count}");

        var loanId = $"L-{DateTime.Now:HHmmss}";
        service.IssueCopy(loanId, added.Id, "R-01", today);
        Console.WriteLine($"Видано {added.Id}, виданий = {store.GetById(added.Id)!.IsIssued}");

        service.ReturnCopy(loanId, today);
        Console.WriteLine($"Повернено {added.Id}, виданий = {store.GetById(added.Id)!.IsIssued}");

        // --- Сценарій 2: відмови без stack trace ---
        Console.WriteLine("\n== Сценарій 2: відмови ==");
        TryDo("Дубль id", () => store.Add(BookCopy.Create(added.Id, isbn)));
        TryDo("Неіснуючий примірник", () => service.IssueCopy("L-X", "NO-SUCH-ID", "R-01", today));
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"{title}: помилки не було");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{title} -> {ex.GetType().Name}: {ex.Message}");
        }
    }
}