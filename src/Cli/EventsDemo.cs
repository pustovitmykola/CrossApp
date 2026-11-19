using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class EventsDemo
{
    private static readonly string LogPath =
        Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

    public static void Run(string[] args)
    {
        // composition root з Лаб.5: сховище обирається аргументами --file / --cache
        var store = StoreFactory.Create(args);
        var service = new LendingService(store);
        Console.WriteLine($"Сховище: {store.GetType().Name}");

        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        File.WriteAllText(LogPath, string.Empty);   // чистий лог для відтворюваного показу

        // підписник 1: іменований метод (його можна зняти через -=)
        service.Changed += HandleChange;

        // підписник 2: лямбда із замиканням над локальною змінною
        int eventCount = 0;
        service.Changed += (_, _) => eventCount++;

        var today = DateOnly.FromDateTime(DateTime.Today);

        Console.WriteLine("\n=== Сценарій: події ===");
        var copy = service.AddBook("9780132350884");
        service.IssueCopy("L-E1", copy.Id, "R-01", today);
        service.ReturnCopy("L-E1", today);
        Console.WriteLine($"Подій отримано: {eventCount}");

        Console.WriteLine("\n=== Сценарій відмови ===");
        TryDo("видача неіснуючого примірника", () => service.IssueCopy("L-E2", "NO-SUCH-ID", "R-01", today));
        TryDo("повернення неіснуючої видачі", () => service.ReturnCopy("L-999", today));
        TryDo("повторне повернення", () => service.ReturnCopy("L-E1", today));
        Console.WriteLine($"Подій отримано: {eventCount} (відмови подій не створили)");

        Console.WriteLine("\n=== Після відписки ===");
        service.Changed -= HandleChange;
        service.IssueCopy("L-E3", copy.Id, "R-01", today);   // виконується, але консоль і лог мовчать
        Console.WriteLine($"Подій отримано: {eventCount} (+1 лише від лямбди-лічильника)");

        Console.WriteLine("\n=== FindAll із Func ===");
        Console.WriteLine($"Вільних: {service.FindAll(c => !c.IsIssued).Count}");
        Console.WriteLine($"Виданих: {service.FindAll(c => c.IsIssued).Count}");
        Console.WriteLine($"Лог: {LogPath}");
    }

    private static void HandleChange(object? sender, LendingChangedEventArgs e)
    {
        var line = LendingMessages.Format(e);
        Console.WriteLine(line);
        File.AppendAllText(LogPath, line + Environment.NewLine);
    }

    private static void TryDo(string title, Action action)
    {
        try { action(); Console.WriteLine($"{title}: помилки не було"); }
        catch (Exception ex) { Console.WriteLine($"{title} -> {ex.GetType().Name}: {ex.Message}"); }
    }
}