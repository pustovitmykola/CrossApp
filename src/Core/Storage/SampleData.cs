using Core.Domain;

namespace Core.Storage;

public sealed record ReportSet(
    IReadOnlyList<Book> Books,
    IReadOnlyList<BookCopy> Copies,
    IReadOnlyList<Loan> Loans);

public static class SampleData
{
    // Фіксована дата звіту, щоб вивід був відтворюваним
    public static readonly DateOnly ReferenceDate = new(2026, 11, 20);

    // Початкові примірники з попередніх лабораторних (без змін)
    public static IEnumerable<BookCopy> Copies() => new[]
    {
        BookCopy.Create("C-001", "9780132350884"),
        BookCopy.Create("C-002", "9780596517748"),
        BookCopy.Create("C-003", "9781491950296"),
    };

    private static string Isbn(int n) => $"978966000{n:D4}";

    // Дані для лабораторної 7: 15 видань, 18 примірників, 14 видач, 5 читачів
    public static ReportSet Report()
    {
        var books = new List<Book>
        {
            Book.Create("B-01", Isbn(1),  "Кобзар", 1840, "Тарас Шевченко"),
            Book.Create("B-02", Isbn(2),  "Хіба ревуть воли, як ясла повні?", 1880, "Панас Мирний"),
            Book.Create("B-03", Isbn(3),  "Захар Беркут", 1883, "Іван Франко"),
            Book.Create("B-04", Isbn(4),  "Лис Микита", 1890, "Іван Франко"),
            Book.Create("B-05", Isbn(5),  "Intermezzo", 1908, "Михайло Коцюбинський"),
            Book.Create("B-06", Isbn(6),  "Fata morgana", 1910, "Михайло Коцюбинський"),
            Book.Create("B-07", Isbn(7),  "Лісова пісня", 1911, "Леся Українка"),
            Book.Create("B-08", Isbn(8),  "Тіні забутих предків", 1911, "Михайло Коцюбинський"),
            Book.Create("B-09", Isbn(9),  "Маруся Чурай", 1979, "Ліна Костенко"),
            Book.Create("B-10", Isbn(10), "Місто", 1928, "Валер'ян Підмогильний"),
            Book.Create("B-11", Isbn(11), "Мина Мазайло", 1929, "Микола Куліш"),
            Book.Create("B-12", Isbn(12), "Земля", 1902, "Ольга Кобилянська"),
            Book.Create("B-13", Isbn(13), "Тигролови", 1944, "Іван Багряний"),
            Book.Create("B-14", Isbn(14), "Собор", 1968, "Олесь Гончар"),
            Book.Create("B-15", Isbn(15), "Людина і зброя", 1960, "Олесь Гончар"),
        };

        // 18 примірників на 10 видань (від 1 до 3 на видання); видання 4, 6, 9, 11, 15 без примірників
        var copies = new List<BookCopy>();
        void AddCopies(int bookNo, params string[] ids)
        {
            foreach (var id in ids) copies.Add(BookCopy.Create(id, Isbn(bookNo)));
        }
        AddCopies(1,  "C-101", "C-102", "C-103");
        AddCopies(2,  "C-104");
        AddCopies(3,  "C-105", "C-106");
        AddCopies(5,  "C-107", "C-108");
        AddCopies(7,  "C-109", "C-110");
        AddCopies(8,  "C-111", "C-112");
        AddCopies(10, "C-113");
        AddCopies(12, "C-114");
        AddCopies(13, "C-115", "C-116");
        AddCopies(14, "C-117", "C-118");

        BookCopy Copy(string id) => copies.First(c => c.Id == id);
        static DateOnly D(int month, int day) => new(2026, month, day);

        var loans = new List<Loan>();
        void Open(string id, string copyId, string reader, DateOnly issued) =>
            loans.Add(Loan.Open(id, Copy(copyId), reader, issued));
        void Returned(string id, string copyId, string reader, DateOnly issued, DateOnly returned)
        {
            var loan = Loan.Open(id, Copy(copyId), reader, issued);
            loan.Close(returned);
            loans.Add(loan);
        }

        // Відкриті видачі (10). Строк = видача + 14 днів, дата звіту 2026-11-20
        Open("L-01", "C-102", "R-001", D(10, 10)); // прострочено на 27 дн.
        Open("L-02", "C-104", "R-001", D(11, 10));
        Open("L-03", "C-107", "R-001", D(11, 12));
        Open("L-04", "C-105", "R-002", D(10, 20)); // прострочено на 17 дн.
        Open("L-05", "C-108", "R-002", D(11, 6));  // строк рівно 11-20: НЕ прострочена
        Open("L-06", "C-110", "R-002", D(11, 15));
        Open("L-07", "C-111", "R-003", D(11, 1));  // прострочено на 5 дн.
        Open("L-08", "C-113", "R-003", D(11, 18));
        Open("L-09", "C-115", "R-004", D(11, 4));  // прострочено на 2 дн.
        Open("L-10", "C-117", "R-005", D(11, 8));

        // Повернені видачі (4)
        Returned("L-11", "C-101", "R-001", D(9, 1),  D(9, 10));
        Returned("L-12", "C-103", "R-002", D(9, 5),  D(9, 20));
        Returned("L-13", "C-114", "R-003", D(9, 12), D(9, 25));
        Returned("L-14", "C-116", "R-004", D(9, 15), D(9, 30));

        // R-001 і R-002 мають по 4 видачі (нічия), R-003 - 3, R-004 - 2, R-005 - 1
        return new ReportSet(books, copies, loans);
    }
}
