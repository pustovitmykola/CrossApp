using Core.Dto;

namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public BookCopy Copy { get; }
    public string ReaderId { get; }
    public DateOnly IssuedOn { get; }
    public DateOnly? ReturnedOn { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan(string id, BookCopy copy, string readerId,
                 DateOnly issuedOn, DateOnly? returnedOn, LoanStatus status)
    {
        Id = id;
        Copy = copy;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
        Status = status;
    }

    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ValidateCommon(id, copy, readerId, issuedOn);
        copy.Issue();
        return new Loan(id.Trim(), copy, readerId.Trim(), issuedOn, null, LoanStatus.Open);
    }

    public void Close(DateOnly returnedOn)
    {
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення {returnedOn:yyyy-MM-dd} раніша за дату видачі {IssuedOn:yyyy-MM-dd}");

        EnsureTransition(LoanStatus.Returned);

        Copy.Return();
        ReturnedOn = returnedOn;
        Status = LoanStatus.Returned;
    }

    public void MarkLost()
    {
        EnsureTransition(LoanStatus.Lost);
        Status = LoanStatus.Lost;
    }

    private static bool IsAllowed(LoanStatus from, LoanStatus to) => (from, to) switch
    {
        (LoanStatus.Open, LoanStatus.Returned) => true,
        (LoanStatus.Open, LoanStatus.Lost) => true,
        (LoanStatus.Lost, LoanStatus.Returned) => true,
        _ => false
    };

    private void EnsureTransition(LoanStatus to)
    {
        if (Status == LoanStatus.Returned)
            throw new InvalidOperationException($"Видача {Id} вже закрита, змінювати її не можна");
        if (!IsAllowed(Status, to))
            throw new InvalidOperationException(
                $"Видача {Id}: перехід {Status} → {to} заборонений");
    }

    private static void ValidateCommon(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(id));
        if (copy is null)
            throw new ArgumentNullException(nameof(copy), "Примірник обов'язковий");
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача не може бути порожнім", nameof(readerId));
        if (issuedOn == default)
            throw new ArgumentOutOfRangeException(nameof(issuedOn), issuedOn,
                "Дата видачі не задана");
    }

    public LoanDto ToDto() =>
        new(Id, Copy.Id, ReaderId, IssuedOn, ReturnedOn, Status.ToString());

    public static Loan FromDto(LoanDto dto, BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateCommon(dto.Id, copy, dto.ReaderId, dto.IssuedOn);

        if (copy.Id != dto.CopyId)
            throw new ArgumentException(
                $"Видача {dto.Id} посилається на примірник {dto.CopyId}, а передано {copy.Id}", nameof(copy));

        if (!Enum.TryParse<LoanStatus>(dto.Status, out var status) || !Enum.IsDefined(status))
            throw new ArgumentException($"Невідомий статус видачі: '{dto.Status}'", nameof(dto));

        if (status == LoanStatus.Returned && dto.ReturnedOn is null)
            throw new ArgumentException($"Видача {dto.Id} повернена, але дати повернення немає", nameof(dto));
        if (status != LoanStatus.Returned && dto.ReturnedOn is not null)
            throw new ArgumentException($"Видача {dto.Id} має статус {status}, але має дату повернення", nameof(dto));
        if (dto.ReturnedOn is { } returned && returned < dto.IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(dto), returned,
                $"Дата повернення {returned:yyyy-MM-dd} раніша за дату видачі {dto.IssuedOn:yyyy-MM-dd}");
        if (status != LoanStatus.Returned && !copy.IsIssued)
            throw new InvalidOperationException(
                $"Видача {dto.Id} не закрита, але примірник {copy.Id} вільний");

        return new Loan(dto.Id.Trim(), copy, dto.ReaderId.Trim(), dto.IssuedOn, dto.ReturnedOn, status);
    }

    public override string ToString() =>
        $"{Id}: читач {ReaderId}, примірник {Copy.Id}, {Status}, з {IssuedOn:yyyy-MM-dd}" +
        (ReturnedOn is { } r ? $" по {r:yyyy-MM-dd}" : "");
}