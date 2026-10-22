using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class BookEntityMapper
{
    public static ImportResult<Book> ToEntities(ImportResult<BookDto> source)
    {
        var books = new List<Book>();
        var errors = new List<string>(source.Errors);

        foreach (var dto in source.Items)
        {
            try
            {
                books.Add(Book.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{dto.Id} ({dto.Isbn}): {ex.Message}");
            }
        }

        return new ImportResult<Book>(books, errors);
    }
}