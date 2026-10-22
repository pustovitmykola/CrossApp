using Core.Dto;

namespace Core.Domain;

public sealed class Book
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public int Year { get; }
    public string? Author { get; }

    private Book(string id, string isbn, string title, int year, string? author)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        Year = year;
        Author = author;
    }

    public static Book Create(string id, string isbn, string title, int year, string? author = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор книги обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        var digits = isbn.Replace("-", "").Trim();
        if (digits.Length is not (10 or 13) || !digits.All(char.IsDigit))
            throw new ArgumentException(
                $"ISBN '{isbn}' має містити 10 або 13 цифр", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва не може бути порожньою", nameof(title));
        if (year is < 1450 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), year,
                "Рік видання має бути в межах 1450–2100");

        return new Book(id.Trim(), digits, title.Trim(), year,
            string.IsNullOrWhiteSpace(author) ? null : author.Trim());
    }

    public BookDto ToDto() => new(Id, Isbn, Title, Year, Author);

    public static Book FromDto(BookDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title, dto.Year, dto.Author);

    public override string ToString() => $"{Id} [{Isbn}] {Title} ({Year})";
}