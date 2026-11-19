using Core.Domain;

namespace Core.Abstractions;

/// <summary>Контракт сховища примірників: що вміє, але не як.</summary>
public interface IBookCopyStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);      // дубль id -> InvalidOperationException
    void Update(BookCopy item);   // немає запису -> InvalidOperationException
    void Remove(string id);       // немає запису -> InvalidOperationException

    // ДОДАТКОВЕ 2: default interface method — готова реалізація, реалізаціям нічого писати не треба
    IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)
        => List().Where(predicate).ToList();
}