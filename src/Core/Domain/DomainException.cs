namespace Core.Domain;

/// <summary>Порушення бізнес-правил (ліміт видач, повторне повернення, дубль id, некоректний запис імпорту).
/// Некоректні аргументи методів лишаються за ArgumentException.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
