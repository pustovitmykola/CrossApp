namespace Core.Dto;

public sealed record MixedImportResult(
    IReadOnlyList<BookDto> Books,
    IReadOnlyList<ReaderDto> Readers,
    IReadOnlyList<string> Errors)
{
    public ImportStats Stats => new(
        Books.Count + Readers.Count + Errors.Count,
        Books.Count + Readers.Count,
        Errors.Count);
}