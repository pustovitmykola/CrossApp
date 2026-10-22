namespace Core.Dto;

public sealed record ImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors)
{
    public ImportStats Stats => new(Items.Count + Errors.Count, Items.Count, Errors.Count);
}