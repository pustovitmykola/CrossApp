\# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.

Призначення: облік видач примірників книг читачам і повернень.



\## Запуск

dotnet build

dotnet run --project src/Cli



\## Середовище

.NET SDK 8.0.400, Windows 11 Pro x64 (RID: win-x64)


\## Додаткове завдання: self-contained публікація під різні RID

| RID | Розмір publish (байти) | Розмір (МБ) |

|---|---|---|

| win-x64 | 74 072 552 | ≈ 70.64 МБ |

| linux-x64 | 73 986 213 | ≈ 70.56 МБ |



Розміри практично однакові, оскільки self-contained публікація включає повний .NET runtime незалежно від цільової платформи.

## Формат вхідних даних (lab03)

Файл `data/sample.csv`:

- роздільник колонок: крапка з комою (`;`)
- перший рядок — заголовок `id;isbn;title;year;author`
- кодування: UTF-8
- колонка `author` необов'язкова (у рядку 4 або 5 колонок)
- порожні рядки та рядки, що починаються з `#`, ігноруються
- рік — ціле число від 1450 до поточного року

Запуск:

    dotnet run --project src/Cli
    dotnet run --project src/Cli -- data/broken.csv
    
    ## Додаткові формати (lab03, додаткове завдання)

- `data/sample.json` — масив JSON-об'єктів `BookDto`; імпортер обирається за розширенням (`.csv` / `.json`)
- `data/mixed.csv` — рядки з префіксом типу: `B;id;isbn;title;year[;author]` — книга, `R;id;fullName[;email]` — читач

Запуск:

    dotnet run --project src/Cli -- data/sample.json
    dotnet run --project src/Cli -- data/mixed.csv --mixed

Після імпорту виводиться статистика: усього / прийнято / пропущено / % помилок.

## Лабораторна 4 — доменна модель (Бібліотека)

Інваріанти:
- Id примірника, Id видачі, ISBN, Id читача не порожні — `ArgumentException` — `BookCopy.Create`, `Loan.Open`, `Loan.FromDto`
- Примірник не можна видати повторно — `InvalidOperationException` — `BookCopy.Issue`
- Примірник не можна повернути, якщо він не виданий — `InvalidOperationException` — `BookCopy.Return`
- Дата повернення не раніша за дату видачі — `ArgumentOutOfRangeException` — `Loan.Close`, `Loan.FromDto`
- Закриту видачу не можна змінювати повторно — `InvalidOperationException` — `Loan.Close`, `Loan.MarkLost`
- Дозволені переходи Open→Returned, Open→Lost, Lost→Returned — `InvalidOperationException` — `Loan.EnsureTransition`
- Не більше 5 відкритих видач на читача — `InvalidOperationException` — `LendingService.IssueCopy`
- ISBN книги має містити 10 або 13 цифр — `ArgumentException` — `Book.Create`
## Лаб. 5: сховище
IBookCopyStore: контракт сховища (List, GetById, Add, Update, Remove).
Реалізації: InMemoryBookCopyStore (в пам'яті) і FileBookCopyStore (JSON-файл).
LendingService залежить лише від інтерфейсу, сховище вибирається в LendingDemo (ручний DI).
Запуск: dotnet run --project src/Cli -- --lending [--file]
$text = @'

## Лабораторна робота 7. Звіти через LINQ

Звіти реалізовано в `Core/Reports/ReportService.cs` (статичний клас, лише LINQ to Objects). Кожен метод приймає `IEnumerable<T>`, повертає `IReadOnlyList` з DTO та закінчується `ToList()`.

| Команда | Звіт | Оператори |
| --- | --- | --- |
| `report-top` | топ-N читачів за кількістю видач | GroupBy, Select, OrderByDescending, ThenBy, Take |
| `report-overdue` | прострочені видачі | Where, Select (DTO), Where, OrderByDescending, ThenBy |
| `report-groups` | видання за десятиліттям | GroupBy, OrderBy, Select |
| `report-sum` | агрегати за примірниками | GroupBy, Select, Count, Average, Max |
| `report-deferred` | демо відкладеного виконання | Where, Count, ToList |
| `report-empty` | звіти на порожньому джерелі | усі звіти |

Запуск: `dotnet run --project src/Cli -- report-top`.

### Відкладене виконання

Запит `Where` лише описується в момент оголошення і виконується тоді, коли його перебирають або викликають завершальний оператор. У `report-deferred` перший `Count()` дає 8, після додавання примірника в джерело другий `Count()` виконує запит заново і дає 9, бо запит завжди бачить поточний стан джерела. `ToList()` виконує запит один раз і відв'язує результат від джерела: знімок містить 9 елементів і не змінюється після `Clear()`, тоді як сам запит після очищення бачить 0.
'@
[System.IO.File]::AppendAllText("$PWD\README.md", $text, [System.Text.UTF8Encoding]::new($false))

git add .
git commit -m "lab07: LINQ reports (top readers, overdue, books by decade, copy totals), deferred execution demo"
git push