using Core.Abstractions;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class EventsExtraDemo
{
    public static void Run(string[] args)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // ---------- Додаткове 1: INotificationSink ----------
        Console.WriteLine("=== Додаткове 1: INotificationSink ===");
        var sinkLog = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
        Directory.CreateDirectory(Path.GetDirectoryName(sinkLog)!);
        File.WriteAllText(sinkLog, string.Empty);

        var listSink = new ListSink();
        INotificationSink sink = new CompositeSink(new ConsoleSink(), new FileSink(sinkLog), listSink);

        var service = new LendingService(StoreFactory.Create(args), sink);

        int viaEvent = 0;
        service.Changed += (_, _) => viaEvent++;   // подія й sink працюють паралельно

        var copy = service.AddBook("9780132350884");
        service.IssueCopy("L-X1", copy.Id, "R-02", today);
        service.ReturnCopy("L-X1", today);

        Console.WriteLine($"\nПодій через event: {viaEvent}");
        Console.WriteLine($"Рядків у sink.log: {File.ReadAllLines(sinkLog).Length}");

        try { service.ReturnCopy("L-X1", today); }
        catch (Exception ex) { Console.WriteLine($"повторне повернення: {ex.GetType().Name} — {ex.Message}"); }
        Console.WriteLine($"Після відмови рядків у sink.log: {File.ReadAllLines(sinkLog).Length}");
        Console.WriteLine($"ListSink: {listSink.Received.Count} повідомлення, перше — {listSink.Received[0].Kind}");

        // ---------- Додаткове 2: Action<string> в імпорті ----------
        Console.WriteLine("\n=== Додаткове 2: Action<string> в імпорті ===");
        // шлях до CSV підстав свій (той самий, що використовує Program.cs)
        var csv = args.FirstOrDefault(a => a.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                  ?? Path.Combine("data", "sample.csv");

        int skipped = 0;
        var result = BookCsvImporter.Load(csv, message =>
        {
            skipped++;
            Console.WriteLine($"  пропущено: {message}");   // у момент пропуску, а не в кінці
        });
        Console.WriteLine($"Завантажено: {result.Items.Count}, повідомлень про пропуск: {skipped}");
    }
}