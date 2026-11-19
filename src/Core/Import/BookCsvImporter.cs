using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class BookCsvImporter
{
    // Роздільник колонок — крапка з комою
    private const char Separator = ';';

    // перевантаження (а не необов'язковий параметр), щоб метод і далі
    // можна було передавати як Func<string, ImportResult<BookDto>>
    public static ImportResult<BookDto> Load(string path) => Load(path, _ => { });

    public static ImportResult<BookDto> Load(string path, Action<string> onSkipped)
    {
        ArgumentNullException.ThrowIfNull(onSkipped);

        var items = new List<BookDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed bad:
                    var message = $"рядок {number}: {bad.Reason}";
                    errors.Add(message);
                    onSkipped(message);   // повідомляємо в момент пропуску
                    break;
            }
        }
        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 4 } or { Length: > 5 }
                => new ParseFailed($"очікую 4 або 5 колонок, отримав {parts.Length}"),

            [_, "", _, ..] or [_, _, "", ..]
                => new ParseFailed("ISBN або назва порожні"),

            [_, _, _, var year, ..] when
                !int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                || y < 1450 || y > DateTime.Now.Year
                => new ParseFailed($"рік '{year}' поза допустимими межами"),

            [var id, var isbn, var title, var year]
                => new ParseOk(new BookDto(id, isbn, title,
                    int.Parse(year, CultureInfo.InvariantCulture))),

            [var id, var isbn, var title, var year, var author]
                => new ParseOk(new BookDto(id, isbn, title,
                    int.Parse(year, CultureInfo.InvariantCulture), author)),

            _ => new ParseFailed("не вдалося розпізнати рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}