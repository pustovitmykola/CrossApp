using System.Text.Json;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Cli;

/// <summary>Лаб.8. Використання: Cli --async [шлях.json] [--timeout N] [--slow] [--stream]
/// Ctrl+C скасовує імпорт; --timeout N скасовує за N секунд; --slow додає 400 мс на запис.</summary>
public static class AsyncDemo
{
    public static async Task<int> RunAsync(string[] args)
    {
        string path = args.FirstOrDefault(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                      ?? Path.Combine("data", "copies.json");
        bool slow = args.Contains("--slow");
        bool stream = args.Contains("--stream");
        int timeoutSec = ReadInt(args, "--timeout") ?? 0;

        var store = new InMemoryBookCopyStore(SampleData.Copies());
        var service = new LendingService(store);
        int events = 0;
        service.Changed += (_, e) => { if (e.Kind == LendingChangeKind.BookAdded) events++; };

        var importer = new JsonCopyImporter(slow ? TimeSpan.FromMilliseconds(400) : null);
        var progress = new ConsoleProgress();

        // зв'язані токени: скасування від Ctrl+C і від таймауту; причину розрізняємо за окремим джерелом
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = timeoutSec > 0
            ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec))
            : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; userCts.Cancel(); };
        Console.CancelKeyPress += onCancel;

        int before = store.List().Count;
        Console.WriteLine($"Імпорт із {path} (у сховищі {before} примірників; Ctrl+C для скасування)");

        try
        {
            ImportReport report = stream
                ? await service.ImportStreamAsync(importer.StreamAsync(path, linked.Token), linked.Token)
                : await service.ImportAsync(importer, path, progress, linked.Token);

            Console.WriteLine($"Додано: {report.Added}, пропущено: {report.Skipped}, подій BookAdded: {events}");

            var again = stream
                ? await service.ImportStreamAsync(importer.StreamAsync(path, linked.Token), linked.Token)
                : await service.ImportAsync(importer, path, null, linked.Token);
            Console.WriteLine($"Повторний імпорт: додано {again.Added}, пропущено {again.Skipped} (ідемпотентність)");
            return 0;
        }
        catch (OperationCanceledException)
        {
            string why = timeoutCts.IsCancellationRequested ? $"таймаут {timeoutSec} с" : "Ctrl+C";
            Console.WriteLine($"Імпорт скасовано ({why}). У сховищі {store.List().Count} примірників (було {before}).");
            return 3;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Порушення правил: {ex.Message}");
            Console.WriteLine($"Сховище не змінено: {store.List().Count} примірників (було {before}).");
            return 4;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Технічна помилка ({ex.GetType().Name}): {ex.Message}");
            return 5;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    // Progress<T> повідомляє асинхронно і міг би вивести рядки вже після підсумку; тут потрібен порядок
    private sealed class ConsoleProgress : IProgress<int>
    {
        public void Report(int n) => Console.WriteLine($"  прочитано записів: {n}");
    }

    private static int? ReadInt(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v) ? v : null;
    }
}
