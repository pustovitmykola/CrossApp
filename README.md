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

