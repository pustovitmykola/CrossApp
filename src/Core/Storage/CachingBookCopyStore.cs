using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// ДОДАТКОВЕ 1: декоратор. Реалізує той самий інтерфейс і обгортає інше сховище.
/// Кешує List() до першої зміни (Add/Update/Remove).
/// </summary>
public class CachingBookCopyStore : IBookCopyStore
{
    private readonly IBookCopyStore _inner;
    private IReadOnlyList<BookCopy>? _listCache;

    public CachingBookCopyStore(IBookCopyStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public IBookCopyStore Inner => _inner;

    public IReadOnlyList<BookCopy> List() => _listCache ??= _inner.List();

    public BookCopy? GetById(string id) => _inner.GetById(id);

    public void Add(BookCopy item)
    {
        _inner.Add(item);
        _listCache = null;   // скидаємо кеш
    }

    public void Update(BookCopy item)
    {
        _inner.Update(item);
        _listCache = null;
    }

    public void Remove(string id)
    {
        _inner.Remove(id);
        _listCache = null;
    }
}