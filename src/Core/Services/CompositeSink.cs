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