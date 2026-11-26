using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

/// <summary>Асинхронне читання JSON. FileStream з useAsync: true, щоб читання справді було асинхронним.</summary>
public sealed class JsonCopyImporter : ICopyImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeSpan _perItemDelay;

    /// <param name="perItemDelay">штучна затримка на запис, лише для демонстрації скасування (--slow)</param>
    public JsonCopyImporter(TimeSpan? perItemDelay = null) => _perItemDelay = perItemDelay ?? TimeSpan.Zero;

    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

    public async Task<IReadOnlyList<BookCopyDto>> ReadAsync(
        string path, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = Open(path);
        var list = await JsonSerializer.DeserializeAsync<List<BookCopyDto>>(stream, Options, ct) ?? new();

        for (int i = 0; i < list.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (_perItemDelay > TimeSpan.Zero) await Task.Delay(_perItemDelay, ct);
            progress?.Report(i + 1);
        }
        return list;
    }

    /// <summary>ДОДАТКОВЕ: потокове читання, записи віддаються по одному без завантаження всього файлу.</summary>
    public async IAsyncEnumerable<BookCopyDto> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = Open(path);
        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto>(stream, Options, ct))
        {
            if (dto is null) continue;
            if (_perItemDelay > TimeSpan.Zero) await Task.Delay(_perItemDelay, ct);
            yield return dto;
        }
    }
}
