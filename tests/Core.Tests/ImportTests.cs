using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;

namespace Core.Tests;

public class ImportTests
{
    private static (LendingService svc, FakeStore store, List<LendingChangedEventArgs> events) Make()
    {
        var store = new FakeStore();
        var svc = new LendingService(store);
        var events = new List<LendingChangedEventArgs>();
        svc.Changed += (_, e) => events.Add(e);
        return (svc, store, events);
    }

    [Fact]
    public async Task ImportAsync_ValidFile_AddsAll_AndRaisesBookAddedPerCopy()
    {
        var (svc, store, events) = Make();
        var importer = new FakeImporter(Dtos.Ok("C-101"), Dtos.Ok("C-102", Dtos.Isbn2));

        var report = await svc.ImportAsync(importer, "any.json");

        Assert.Equal(new ImportReport(2, 0), report);
        Assert.Equal(2, store.List().Count);
        Assert.Equal(2, events.Count(e => e.Kind == LendingChangeKind.BookAdded));
    }

    [Fact]
    public async Task ImportAsync_Twice_IsIdempotent()
    {
        var (svc, store, events) = Make();
        var importer = new FakeImporter(Dtos.Ok("C-101"), Dtos.Ok("C-102"));
        await svc.ImportAsync(importer, "any.json");

        var second = await svc.ImportAsync(importer, "any.json");

        Assert.Equal(new ImportReport(0, 2), second);
        Assert.Equal(2, store.List().Count);
        Assert.Equal(2, events.Count);   // повторний імпорт нових подій не дав
    }

    [Fact]
    public async Task ImportAsync_BadRecord_ThrowsDomainException_AndStoreUntouched()
    {
        var (svc, store, events) = Make();
        var importer = new FakeImporter(Dtos.Ok("C-201"), new BookCopyDto("C-202", "", false), Dtos.Ok("C-203"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => svc.ImportAsync(importer, "any.json"));

        Assert.Contains("Запис 2", ex.Message);
        Assert.Empty(store.List());   // «усе або нічого»: перший валідний запис теж не потрапив
        Assert.Empty(events);
    }

    [Fact]
    public async Task ImportAsync_DuplicateIdsInFile_ThrowsDomainException()
    {
        var (svc, store, _) = Make();
        var importer = new FakeImporter(Dtos.Ok("C-101"), Dtos.Ok("C-101"));

        await Assert.ThrowsAsync<DomainException>(() => svc.ImportAsync(importer, "any.json"));
        Assert.Empty(store.List());
    }

    [Fact]
    public async Task ImportAsync_CanceledToken_ThrowsOperationCanceled_AndStoreUntouched()
    {
        var (svc, store, events) = Make();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.ImportAsync(new FakeImporter(Dtos.Ok("C-101")), "any.json", ct: cts.Token));

        Assert.Empty(store.List());
        Assert.Empty(events);
    }

    [Fact]
    public async Task ImportAsync_ReportsProgress()
    {
        var (svc, _, _) = Make();
        var progress = new ListProgress();

        await svc.ImportAsync(new FakeImporter(Dtos.Ok("C-101"), Dtos.Ok("C-102"), Dtos.Ok("C-103")),
            "any.json", progress);

        Assert.Equal(new[] { 1, 2, 3 }, progress.Values);
    }

    [Fact]
    public async Task ImportAsync_NullImporter_ThrowsArgumentNullException()
    {
        var (svc, _, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.ImportAsync(null!, "any.json"));
    }

    private static async IAsyncEnumerable<BookCopyDto> Stream(params BookCopyDto[] items)
    {
        foreach (var i in items) { await Task.Yield(); yield return i; }
    }

    [Fact]
    public async Task ImportStreamAsync_AddsNewAndSkipsExisting()
    {
        var (svc, store, _) = Make();
        await svc.ImportStreamAsync(Stream(Dtos.Ok("C-101")));

        var report = await svc.ImportStreamAsync(Stream(Dtos.Ok("C-101"), Dtos.Ok("C-102")));

        Assert.Equal(new ImportReport(1, 1), report);
        Assert.Equal(2, store.List().Count);
    }

    // єдиний тест із диском: справжнє асинхронне читання тимчасового файлу
    [Fact]
    public async Task JsonCopyImporter_ReadsTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"copies-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path,
            """[{"Id":"C-901","Isbn":"9780132350884","IsIssued":false},{"Id":"C-902","Isbn":"9789660000001","IsIssued":false}]""");
        try
        {
            var (svc, store, _) = Make();

            var report = await svc.ImportAsync(new JsonCopyImporter(), path);

            Assert.Equal(new ImportReport(2, 0), report);
            Assert.NotNull(store.GetById("C-902"));
        }
        finally { File.Delete(path); }
    }
}
