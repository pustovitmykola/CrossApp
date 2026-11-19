using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class LendingService
{
    public const int MaxOpenLoansPerReader = 5;

    private readonly IBookCopyStore _store;
    private readonly List<Loan> _loans = [];
    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

    // сервіс залежить лише від інтерфейсу; конкретне сховище приходить ззовні
    public LendingService(IBookCopyStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public BookCopy AddBook(string isbn)
    {
        var n = _store.List().Count + 1;
        string id;
        do { id = $"C-{n++:000}"; } while (_store.GetById(id) is not null);

        var copy = BookCopy.Create(id, isbn);
        _store.Add(copy);
        return copy;
    }

    // нове: видача за id примірника зі сховища
    public Loan IssueCopy(string loanId, string copyId, string readerId, DateOnly issuedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new ArgumentException($"Примірник {copyId} не знайдено", nameof(copyId));

        var loan = IssueCopy(loanId, copy, readerId, issuedOn);
        _store.Update(copy);   // зберігаємо новий стан примірника
        return loan;
    }

    // з Лаб.4: приймає сам примірник
    public Loan IssueCopy(string loanId, BookCopy copy, string readerId, DateOnly issuedOn)
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

    public void ReturnCopy(string loanId, DateOnly returnedOn)
    {
        var loan = _loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new InvalidOperationException($"Видачу {loanId} не знайдено");
        loan.Close(returnedOn);

        // примірник може бути й не зі сховища (демо Лаб.4), тоді просто пропускаємо
        if (_store.GetById(loan.Copy.Id) is not null)
            _store.Update(loan.Copy);
    }
}