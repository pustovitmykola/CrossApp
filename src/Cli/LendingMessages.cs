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