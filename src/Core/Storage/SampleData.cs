using Core.Domain;

namespace Core.Storage;

public static class SampleData
{
    public static IEnumerable<BookCopy> Copies() => new[]
    {
        BookCopy.Create("C-001", "9780132350884"),
        BookCopy.Create("C-002", "9780596517748"),
        BookCopy.Create("C-003", "9781491950296"),
    };
}