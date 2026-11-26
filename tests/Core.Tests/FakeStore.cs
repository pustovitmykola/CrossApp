using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

/// <summary>Сховище в пам'яті для тестів: без диска й без залежності від SampleData.</summary>
public sealed class FakeStore : IBookCopyStore
{
    private readonly Dictionary<string, BookCopy> _items = new();

    public IReadOnlyList<BookCopy> List() => _items.Values.ToList();
    public BookCopy? GetById(string id) => _items.TryGetValue(id, out var c) ? c : null;

    public void Add(BookCopy item)
    {
        if (!_items.TryAdd(item.Id, item))
            throw new DomainException($"Примірник з id {item.Id} вже існує");
    }

    public void Update(BookCopy item)
    {
        if (!_items.ContainsKey(item.Id))
            throw new DomainException($"Примірник {item.Id} не знайдено");
        _items[item.Id] = item;
    }

    public void Remove(string id)
    {
        if (!_items.Remove(id))
            throw new DomainException($"Примірник {id} не знайдено");
    }
}
