using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>Дані у JSON-файлі. Файл читається один раз (кеш), після кожної зміни пишеться назад.</summary>
public class FileBookCopyStore : IBookCopyStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;
    private Dictionary<string, BookCopy>? _cache;

    public FileBookCopyStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Шлях до файлу обов'язковий", nameof(path));
        _path = path;
    }

    // читаємо файл лише при першому зверненні
    private Dictionary<string, BookCopy> EnsureLoaded()
    {
        if (_cache is not null) return _cache;

        _cache = new Dictionary<string, BookCopy>();
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var dtos = JsonSerializer.Deserialize<List<BookCopyDto>>(json) ?? new();
            foreach (var dto in dtos)
            {
                var copy = BookCopy.FromDto(dto);   // ті самі перевірки, що й при створенні
                _cache[copy.Id] = copy;
            }
        }
        return _cache;
    }

    // записуємо кеш на диск (на диск іде DTO, а не сама сутність)
    private void Flush()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var dtos = _cache!.Values.Select(c => c.ToDto()).ToList();
        File.WriteAllText(_path, JsonSerializer.Serialize(dtos, Options));
    }

    public IReadOnlyList<BookCopy> List() => EnsureLoaded().Values.ToList();

    public BookCopy? GetById(string id)
        => EnsureLoaded().TryGetValue(id, out var c) ? c : null;

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var items = EnsureLoaded();
        if (items.ContainsKey(item.Id))
            throw new DomainException($"Примірник з id {item.Id} вже існує");
        items[item.Id] = item;
        Flush();
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var items = EnsureLoaded();
        if (!items.ContainsKey(item.Id))
            throw new DomainException($"Примірник {item.Id} не знайдено, оновлювати нічого");
        items[item.Id] = item;
        Flush();
    }

    public void Remove(string id)
    {
        var items = EnsureLoaded();
        if (!items.Remove(id))
            throw new DomainException($"Примірник {id} не знайдено, видаляти нічого");
        Flush();
    }
}