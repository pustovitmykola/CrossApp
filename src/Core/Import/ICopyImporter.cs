using Core.Dto;

namespace Core.Import;

/// <summary>Читає примірники з джерела. Сервіс залежить від інтерфейсу, тож у тестах диск не потрібен.</summary>
public interface ICopyImporter
{
    Task<IReadOnlyList<BookCopyDto>> ReadAsync(
        string path, IProgress<int>? progress = null, CancellationToken ct = default);
}
