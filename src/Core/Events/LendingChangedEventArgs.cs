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