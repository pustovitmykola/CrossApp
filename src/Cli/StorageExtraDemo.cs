using Core.Abstractions;
using Core.Domain;
using Core.Storage;

namespace Cli;

public static class StorageExtraDemo
{
    // обгортка, яка лише рахує реальні звернення до List()
    private sealed class CountingStore : IBookCopyStore
    {
        private readonly IBookCopyStore _inner;
        public int ListCalls { get; private set; }
        public CountingStore(IBookCopyStore inner) => _inner = inner;

        public IReadOnlyList<BookCopy> List() { ListCalls++; return _inner.List(); }
        public BookCopy? GetById(string id) => _inner.GetById(id);
        public void Add(BookCopy item) => _inner.Add(item);
        public void Update(BookCopy item) => _inner.Update(item);
        public void Remove(string id) => _inner.Remove(id);
    }

    public static void Run()
    {
        // --- Додаткове 1: декоратор з кешем ---
        Console.WriteLine("== Додаткове 1: CachingBookCopyStore ==");
        var counting = new CountingStore(new InMemoryBookCopyStore(SampleData.Copies()));
        IBookCopyStore cached = new CachingBookCopyStore(counting);

        cached.List(); cached.List(); cached.List();
        Console.WriteLine($"3 виклики List() -> звернень до внутрішнього сховища: {counting.ListCalls}");

        cached.Add(BookCopy.Create("CACHE-001", "9780132350884"));
        cached.List();
        Console.WriteLine($"Після Add і ще одного List() -> звернень: {counting.ListCalls}");

        // --- Додаткове 2: Find (default interface method) ---
        // змінна має бути типу інтерфейсу, інакше default-метод не видно
        Console.WriteLine("\n== Додаткове 2: Find(c => !c.IsIssued) ==");
        IBookCopyStore store = new InMemoryBookCopyStore(SampleData.Copies());
        if (store.Find(c => !c.IsIssued).Count == 0)
            store.Add(BookCopy.Create("FREE-001", "9780132350884"));

        var freeBefore = store.Find(c => !c.IsIssued);
        Console.WriteLine($"Вільних до видачі: {freeBefore.Count}");

        var copy = freeBefore[0];
        copy.Issue();
        store.Update(copy);
        Console.WriteLine($"Видано {copy.Id}. Вільних після видачі: {store.Find(c => !c.IsIssued).Count}");

        // --- Додаткове 3: StoreFactory ---
        Console.WriteLine("\n== Додаткове 3: StoreFactory.Create ==");
        foreach (var a in new[]
        {
            Array.Empty<string>(),
            new[] { "--file" },
            new[] { "--cache" },
            new[] { "--file", "--cache" }
        })
        {
            var s = StoreFactory.Create(a);
            var name = s is CachingBookCopyStore c
                ? $"CachingBookCopyStore({c.Inner.GetType().Name})"
                : s.GetType().Name;
            var label = a.Length == 0 ? "(без аргументів)" : string.Join(" ", a);
            Console.WriteLine($"{label,-18} -> {name}");
        }
    }
}