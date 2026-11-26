using Core.Dto;
using Core.Import;

namespace Core.Tests;

public sealed class FakeImporter : ICopyImporter
{
    private readonly IReadOnlyList<BookCopyDto> _items;
    public FakeImporter(params BookCopyDto[] items) => _items = items;

    public Task<IReadOnlyList<BookCopyDto>> ReadAsync(
        string path, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        for (int i = 0; i < _items.Count; i++) progress?.Report(i + 1);
        return Task.FromResult(_items);
    }
}

/// <summary>Синхронний IProgress: Progress&lt;T&gt; повідомляє асинхронно, тож у тестах він ненадійний.</summary>
public sealed class ListProgress : IProgress<int>
{
    public List<int> Values { get; } = new();
    public void Report(int value) => Values.Add(value);
}

public static class Dtos
{
    public const string Isbn1 = "9780132350884";
    public const string Isbn2 = "9789660000001";

    public static BookCopyDto Ok(string id, string isbn = Isbn1) => new(id, isbn, false);
}
