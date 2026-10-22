using Core.Domain;

namespace Core.Services;

public sealed class LendingService
{
    public const int MaxOpenLoansPerReader = 5;

    private readonly List<Loan> _loans = [];
    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

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
    }
}