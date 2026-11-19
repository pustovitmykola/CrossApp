# Лаб.6: автоматично додає подію Changed, INotificationSink, Action<string> в імпорті та демо.
# Запуск з кореня репозиторію (там, де папка src):
#   powershell -ExecutionPolicy Bypass -File .\apply-lab6.ps1

$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$src = if (Test-Path (Join-Path $root 'src\Core')) { Join-Path $root 'src' } else { $root }

if (-not ((Test-Path (Join-Path $src 'Core')) -and (Test-Path (Join-Path $src 'Cli')))) {
    Write-Host 'Не знайдено Core і Cli. Запусти скрипт з кореня репозиторію (де src\Core і src\Cli).' -ForegroundColor Red
    exit 1
}

$utf8 = New-Object System.Text.UTF8Encoding($false)

function Write-Src($rel, $content) {
    $path = Join-Path $src $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null
    if ((Test-Path $path) -and -not (Test-Path "$path.bak")) { Copy-Item $path "$path.bak" }
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    Write-Host "  записано $rel"
}

Write-Host "Папка проєкту: $src"
Write-Host 'Записую файли...'

Write-Src 'Core/Events/LendingChangeKind.cs' @'
namespace Core.Events;

/// <summary>Які зміни стану бібліотеки вважаємо подіями.</summary>
public enum LendingChangeKind
{
    BookAdded,
    CopyIssued,
    CopyReturned
}
'@

Write-Src 'Core/Events/LendingChangedEventArgs.cs' @'
namespace Core.Events;

public sealed class LendingChangedEventArgs : EventArgs
{
    public string CopyId { get; }
    public LendingChangeKind Kind { get; }
    public string? ReaderId { get; }      // немає для BookAdded
    public DateOnly? DueDate { get; }     // лише для CopyIssued
    public DateTime At { get; } = DateTime.Now;   // фіксується один раз при створенні

    public LendingChangedEventArgs(string copyId, LendingChangeKind kind,
                                   string? readerId = null, DateOnly? dueDate = null)
    {
        CopyId = copyId;
        Kind = kind;
        ReaderId = readerId;
        DueDate = dueDate;
    }
}
'@

Write-Src 'Core/Abstractions/INotificationSink.cs' @'
using Core.Events;

namespace Core.Abstractions;

/// <summary>Куди сервіс відправляє сповіщення; як саме їх показати — справа реалізації.</summary>
public interface INotificationSink
{
    void Notify(LendingChangedEventArgs e);
}
'@

Write-Src 'Core/Services/CompositeSink.cs' @'
using Core.Abstractions;
using Core.Events;

namespace Core.Services;

/// <summary>Розсилає одне сповіщення кільком sink одночасно.</summary>
public sealed class CompositeSink : INotificationSink
{
    private readonly INotificationSink[] _sinks;

    public CompositeSink(params INotificationSink[] sinks) => _sinks = sinks;

    public void Notify(LendingChangedEventArgs e)
    {
        foreach (var sink in _sinks)
            sink.Notify(e);
    }
}
'@

Write-Src 'Core/Services/LendingService.cs' @'
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService
{
    public const int MaxOpenLoansPerReader = 5;
    public const int LoanPeriodDays = 14;   // спрощення: Loan не зберігає термін повернення

    private readonly IBookCopyStore _store;
    private readonly INotificationSink? _sink;
    private readonly List<Loan> _loans = [];
    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

    // event, а не публічне поле: ззовні можна лише += та -=
    public event EventHandler<LendingChangedEventArgs>? Changed;

    // сервіс залежить лише від інтерфейсів; сховище (і необов'язковий sink) приходять ззовні
    public LendingService(IBookCopyStore store, INotificationSink? sink = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _sink = sink;
    }

    // єдине місце, де піднімається подія
    private void OnChanged(LendingChangedEventArgs e)
    {
        Changed?.Invoke(this, e);   // ?. бо без підписників делегат == null
        _sink?.Notify(e);
    }

    public BookCopy AddBook(string isbn)
    {
        var n = _store.List().Count + 1;
        string id;
        do { id = $"C-{n++:000}"; } while (_store.GetById(id) is not null);

        var copy = BookCopy.Create(id, isbn);
        _store.Add(copy);
        OnChanged(new LendingChangedEventArgs(copy.Id, LendingChangeKind.BookAdded));
        return copy;
    }

    // видача за id примірника зі сховища
    public Loan IssueCopy(string loanId, string copyId, string readerId, DateOnly issuedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new ArgumentException($"Примірник {copyId} не знайдено", nameof(copyId));

        var loan = IssueCore(loanId, copy, readerId, issuedOn);
        _store.Update(copy);   // зберігаємо новий стан примірника
        RaiseIssued(loan, issuedOn);   // подія — лише після Update
        return loan;
    }

    // з Лаб.4: приймає сам примірник
    public Loan IssueCopy(string loanId, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        var loan = IssueCore(loanId, copy, readerId, issuedOn);
        RaiseIssued(loan, issuedOn);
        return loan;
    }

    // спільна логіка без події, щоб перше перевантаження не піднімало подію до store.Update
    private Loan IssueCore(string loanId, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        var reader = readerId?.Trim();
        var openCount = _loans.Count(l => l.ReaderId == reader && l.Status == LoanStatus.Open);
        if (openCount >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач {reader} уже має {openCount} відкритих видач (ліміт {MaxOpenLoansPerReader})");

        var loan = Loan.Open(loanId, copy, readerId!, issuedOn);
        _loans.Add(loan);
        return loan;
    }

    private void RaiseIssued(Loan loan, DateOnly issuedOn) =>
        OnChanged(new LendingChangedEventArgs(
            loan.Copy.Id, LendingChangeKind.CopyIssued,
            loan.ReaderId, issuedOn.AddDays(LoanPeriodDays)));

    public void ReturnCopy(string loanId, DateOnly returnedOn)
    {
        var loan = _loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new InvalidOperationException($"Видачу {loanId} не знайдено");
        loan.Close(returnedOn);

        // примірник може бути й не зі сховища (демо Лаб.4), тоді просто пропускаємо
        if (_store.GetById(loan.Copy.Id) is not null)
            _store.Update(loan.Copy);

        OnChanged(new LendingChangedEventArgs(
            loan.Copy.Id, LendingChangeKind.CopyReturned, loan.ReaderId));
    }

    // Func: критерій задає викликач, результат повертається значенням
    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.Find(predicate);   // default-метод з Лаб.5
    }
}
'@

Write-Src 'Core/Import/BookCsvImporter.cs' @'
using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class BookCsvImporter
{
    // Роздільник колонок — крапка з комою
    private const char Separator = ';';

    // перевантаження (а не необов'язковий параметр), щоб метод і далі
    // можна було передавати як Func<string, ImportResult<BookDto>>
    public static ImportResult<BookDto> Load(string path) => Load(path, _ => { });

    public static ImportResult<BookDto> Load(string path, Action<string> onSkipped)
    {
        ArgumentNullException.ThrowIfNull(onSkipped);

        var items = new List<BookDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed bad:
                    var message = $"рядок {number}: {bad.Reason}";
                    errors.Add(message);
                    onSkipped(message);   // повідомляємо в момент пропуску
                    break;
            }
        }
        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 4 } or { Length: > 5 }
                => new ParseFailed($"очікую 4 або 5 колонок, отримав {parts.Length}"),

            [_, "", _, ..] or [_, _, "", ..]
                => new ParseFailed("ISBN або назва порожні"),

            [_, _, _, var year, ..] when
                !int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                || y < 1450 || y > DateTime.Now.Year
                => new ParseFailed($"рік '{year}' поза допустимими межами"),

            [var id, var isbn, var title, var year]
                => new ParseOk(new BookDto(id, isbn, title,
                    int.Parse(year, CultureInfo.InvariantCulture))),

            [var id, var isbn, var title, var year, var author]
                => new ParseOk(new BookDto(id, isbn, title,
                    int.Parse(year, CultureInfo.InvariantCulture), author)),

            _ => new ParseFailed("не вдалося розпізнати рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
'@

Write-Src 'Cli/LendingMessages.cs' @'
using Core.Events;

namespace Cli;

/// <summary>Єдиний формат рядка для всіх обробників і sink.</summary>
public static class LendingMessages
{
    public static string Format(LendingChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-12} " + e.Kind switch
        {
            LendingChangeKind.BookAdded    => $"додано примірник {e.CopyId}",
            LendingChangeKind.CopyIssued   => $"примірник {e.CopyId} видано читачу {e.ReaderId} до {e.DueDate:yyyy-MM-dd}",
            LendingChangeKind.CopyReturned => $"примірник {e.CopyId} повернено читачем {e.ReaderId}",
            _ => $"невідома зміна {e.CopyId}"
        };
}
'@

Write-Src 'Cli/ConsoleSink.cs' @'
using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LendingChangedEventArgs e) => Console.WriteLine(LendingMessages.Format(e));
}
'@

Write-Src 'Cli/FileSink.cs' @'
using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public void Notify(LendingChangedEventArgs e) =>
        File.AppendAllText(_path, LendingMessages.Format(e) + Environment.NewLine);
}
'@

Write-Src 'Cli/ListSink.cs' @'
using Core.Abstractions;
using Core.Events;

namespace Cli;

/// <summary>Тестовий sink: збирає повідомлення в список, без консолі й файлу.</summary>
public sealed class ListSink : INotificationSink
{
    public List<LendingChangedEventArgs> Received { get; } = [];
    public void Notify(LendingChangedEventArgs e) => Received.Add(e);
}
'@

Write-Src 'Cli/EventsDemo.cs' @'
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
'@

Write-Src 'Cli/EventsExtraDemo.cs' @'
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
'@

# ---------- Program.cs: додаємо режими --events та --events-extra ----------
Write-Host 'Оновлюю Program.cs...'
$prog = Join-Path $src 'Cli\Program.cs'
$manual = $false
if (-not (Test-Path $prog)) {
    $manual = $true
}
else {
    $text = [System.IO.File]::ReadAllText($prog)
    if ($text -match 'EventsDemo') {
        Write-Host '  Program.cs уже містить --events, пропускаю'
    }
    elseif (($text -match 'static\s+[\w<>\[\]]+\s+Main') -or ($text -match '(?m)^\s*namespace\s') -or ($text -match 'return\s+[^;\s]')) {
        $manual = $true
    }
    else {
        Copy-Item $prog "$prog.bak" -Force
        $lines = New-Object 'System.Collections.Generic.List[string]'
        $lines.AddRange([System.IO.File]::ReadAllLines($prog))
        $idx = -1
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^\s*using\s+[\w\.]+\s*;') { $idx = $i }
        }
        $ins = @(
            'if (args.Contains("--events")) { Cli.EventsDemo.Run(args); return; }',
            'if (args.Contains("--events-extra")) { Cli.EventsExtraDemo.Run(args); return; }'
        )
        $lines.InsertRange($idx + 1, [string[]]$ins)
        [System.IO.File]::WriteAllLines($prog, $lines, $utf8)
        Write-Host '  додано --events і --events-extra'
    }
}
if ($manual) {
    Write-Host '  Program.cs має нестандартну структуру, додай вручну на початок:' -ForegroundColor Yellow
    Write-Host '    if (args.Contains("--events")) { Cli.EventsDemo.Run(args); return; }' -ForegroundColor Yellow
    Write-Host '    if (args.Contains("--events-extra")) { Cli.EventsExtraDemo.Run(args); return; }' -ForegroundColor Yellow
}

if (-not (Test-Path (Join-Path $src 'Core\Storage\StoreFactory.cs'))) {
    Write-Host 'Увага: не знайдено Core\Storage\StoreFactory.cs. У EventsDemo.cs і EventsExtraDemo.cs заміни StoreFactory.Create(args) на своє створення сховища.' -ForegroundColor Yellow
}

# ---------- Збірка ----------
Write-Host 'Збираю проєкт...'
dotnet build (Join-Path $src 'Cli')
if ($LASTEXITCODE -eq 0) {
    Write-Host ''
    Write-Host 'Готово. Запуск:' -ForegroundColor Green
    Write-Host '  dotnet run --project src/Cli -- --events'
    Write-Host '  dotnet run --project src/Cli -- --events-extra'
    Write-Host 'Старі версії файлів збережено як *.bak (перед комітом їх можна видалити).'
}
else {
    Write-Host 'Збірка не пройшла, скинь мені текст помилки.' -ForegroundColor Red
}
