using Core.Abstractions;
using Core.Events;

namespace Cli;

/// <summary>Тестовий sink: збирає повідомлення в список, без консолі й файлу.</summary>
public sealed class ListSink : INotificationSink
{
    public List<LendingChangedEventArgs> Received { get; } = [];
    public void Notify(LendingChangedEventArgs e) => Received.Add(e);
}