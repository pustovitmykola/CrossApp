using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;

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

    // ---- Лаб.8: асинхронний імпорт ----

    /// <summary>
    /// Імпорт «усе або нічого»: спочатку читання й повна перевірка, і лише потім зміни в сховищі.
    /// Скасування можливе до фази запису (вона синхронна й швидка), тому сховище ніколи не лишається наполовину оновленим.
    /// Ідемпотентність: примірники з id, що вже є у сховищі, пропускаються.
    /// </summary>
    public async Task<ImportReport> ImportAsync(
        ICopyImporter importer, string path,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var dtos = await importer.ReadAsync(path, progress, ct);
        ct.ThrowIfCancellationRequested();

        // фаза 1: перевірка всього файлу, сховище ще не чіпали
        var parsed = new List<BookCopy>(dtos.Count);
        var seen = new HashSet<string>();
        for (int i = 0; i < dtos.Count; i++)
        {
            var copy = ToCopy(dtos[i], i + 1);
            if (!seen.Add(copy.Id))
                throw new DomainException($"Запис {i + 1}: id {copy.Id} повторюється у файлі");
            parsed.Add(copy);
        }

        // фаза 2: запис (без await, тож без точок скасування)
        return Apply(parsed);
    }

    /// <summary>ДОДАТКОВЕ: потоковий імпорт. Записи обробляються по одному, тому при помилці
    /// або скасуванні вже додані примірники лишаються (це не «усе або нічого»).</summary>
    public async Task<ImportReport> ImportStreamAsync(
        IAsyncEnumerable<BookCopyDto> source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        int added = 0, skipped = 0, number = 0;
        await foreach (var dto in source.WithCancellation(ct))
        {
            var copy = ToCopy(dto, ++number);
            if (AddIfNew(copy)) added++; else skipped++;
        }
        return new ImportReport(added, skipped);
    }

    private static BookCopy ToCopy(BookCopyDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній запис");
        try
        {
            return BookCopy.FromDto(dto);   // ті самі перевірки, що й при створенні
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }

    private ImportReport Apply(List<BookCopy> parsed)
    {
        int added = 0, skipped = 0;
        foreach (var copy in parsed)
        {
            if (AddIfNew(copy)) added++; else skipped++;
        }
        return new ImportReport(added, skipped);
    }

    private bool AddIfNew(BookCopy copy)
    {
        if (_store.GetById(copy.Id) is not null) return false;
        _store.Add(copy);
        OnChanged(new LendingChangedEventArgs(copy.Id, LendingChangeKind.BookAdded));
        return true;
    }

    // ---- видачі ----

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
            throw new DomainException(
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
            ?? throw new DomainException($"Видачу {loanId} не знайдено");
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
