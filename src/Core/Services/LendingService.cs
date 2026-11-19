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