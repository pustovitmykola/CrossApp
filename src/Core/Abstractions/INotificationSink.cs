using Core.Events;

namespace Core.Abstractions;

/// <summary>Куди сервіс відправляє сповіщення; як саме їх показати — справа реалізації.</summary>
public interface INotificationSink
{
    void Notify(LendingChangedEventArgs e);
}