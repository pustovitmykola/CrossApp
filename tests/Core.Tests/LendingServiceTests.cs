using Core.Domain;
using Core.Events;
using Core.Services;

namespace Core.Tests;

public class LendingServiceTests
{
    private static readonly DateOnly Day = new(2026, 11, 26);

    private static (LendingService svc, FakeStore store, List<LendingChangedEventArgs> events) Make()
    {
        var store = new FakeStore();
        var svc = new LendingService(store);
        var events = new List<LendingChangedEventArgs>();
        svc.Changed += (_, e) => events.Add(e);
        return (svc, store, events);
    }

    [Fact]
    public void AddBook_AddsToStore_AndRaisesBookAdded()
    {
        var (svc, store, events) = Make();

        var copy = svc.AddBook(Dtos.Isbn1);

        Assert.NotNull(store.GetById(copy.Id));
        var e = Assert.Single(events);
        Assert.Equal(LendingChangeKind.BookAdded, e.Kind);
        Assert.Equal(copy.Id, e.CopyId);
    }

    [Fact]
    public void IssueCopy_ByCopyId_MarksIssued_AndRaisesCopyIssued()
    {
        var (svc, _, events) = Make();
        var copy = svc.AddBook(Dtos.Isbn1);

        svc.IssueCopy("L-1", copy.Id, "R-01", Day);

        Assert.True(copy.IsIssued);
        Assert.Equal(LendingChangeKind.CopyIssued, events[^1].Kind);
        Assert.Equal(Day.AddDays(LendingService.LoanPeriodDays), events[^1].DueDate);
    }

    [Fact]
    public void IssueCopy_UnknownCopyId_ThrowsArgumentException_AndRaisesNothing()
    {
        var (svc, _, events) = Make();

        Assert.Throws<ArgumentException>(() => svc.IssueCopy("L-1", "NO-SUCH", "R-01", Day));
        Assert.Empty(events);
    }

    [Fact]
    public void IssueCopy_OverLimit_ThrowsDomainException()
    {
        var (svc, _, _) = Make();
        for (int i = 1; i <= LendingService.MaxOpenLoansPerReader; i++)
            svc.IssueCopy($"L-{i}", svc.AddBook(Dtos.Isbn1).Id, "R-01", Day);

        var extra = svc.AddBook(Dtos.Isbn1);

        Assert.Throws<DomainException>(() => svc.IssueCopy("L-X", extra.Id, "R-01", Day));
        Assert.False(extra.IsIssued);
    }

    [Fact]
    public void ReturnCopy_UnknownLoan_ThrowsDomainException()
    {
        var (svc, _, _) = Make();
        Assert.Throws<DomainException>(() => svc.ReturnCopy("L-999", Day));
    }

    [Fact]
    public void ReturnCopy_Twice_ThrowsDomainException()
    {
        var (svc, _, _) = Make();
        var copy = svc.AddBook(Dtos.Isbn1);
        svc.IssueCopy("L-1", copy.Id, "R-01", Day);
        svc.ReturnCopy("L-1", Day);

        Assert.Throws<DomainException>(() => svc.ReturnCopy("L-1", Day));
    }

    [Fact]
    public void ReturnCopy_FreesCopy_AndRaisesCopyReturned()
    {
        var (svc, _, events) = Make();
        var copy = svc.AddBook(Dtos.Isbn1);
        svc.IssueCopy("L-1", copy.Id, "R-01", Day);

        svc.ReturnCopy("L-1", Day);

        Assert.False(copy.IsIssued);
        Assert.Equal(LendingChangeKind.CopyReturned, events[^1].Kind);
    }

    [Fact]
    public void FindAll_FiltersByPredicate()
    {
        var (svc, _, _) = Make();
        var a = svc.AddBook(Dtos.Isbn1);
        svc.AddBook(Dtos.Isbn2);
        svc.IssueCopy("L-1", a.Id, "R-01", Day);

        Assert.Single(svc.FindAll(c => c.IsIssued));
        Assert.Single(svc.FindAll(c => !c.IsIssued));
    }
}
