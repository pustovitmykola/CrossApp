using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>Дані лише в пам'яті: зникають після завершення програми.</summary>
public class InMemoryBookCopyStore : IBookCopyStore
{
    private readonly Dictionary<string, BookCopy> _items = new();

    public InMemoryBookCopyStore(IEnumerable<BookCopy>? initial = null)
    {
        if (initial is null) return;
        foreach (var c in initial)
            _items[c.Id] = c;
    }

    public IReadOnlyList<BookCopy> List() => _items.Values.ToList();

    public BookCopy? GetById(string id)
        => _items.TryGetValue(id, out var c) ? c : null;

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new DomainException($"Примірник з id {item.Id} вже існує");
        _items[item.Id] = item;
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new DomainException($"Примірник {item.Id} не знайдено, оновлювати нічого");
        _items[item.Id] = item;
    }

    public void Remove(string id)
    {
        if (!_items.Remove(id))
            throw new DomainException($"Примірник {id} не знайдено, видаляти нічого");
    }
}