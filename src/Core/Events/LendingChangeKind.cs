namespace Core.Events;

/// <summary>Які зміни стану бібліотеки вважаємо подіями.</summary>
public enum LendingChangeKind
{
    BookAdded,
    CopyIssued,
    CopyReturned
}