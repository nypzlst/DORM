using DORM.Infrastructure.TrackHistory;

namespace DORM.Tests;

public class TrackChangesTests
{
    [Fact]
    public void TrackInsert_AddsOperation_AndFiresEvent()
    {
        var sut = new TrackChanges();
        Operation? captured = null;
        sut.OnOperationTracked += op => captured = op;

        sut.TrackInsert(new { X = 1 }, "TUser", "Id");

        Assert.Single(sut.Operations);
        Assert.NotNull(captured);
        Assert.Equal(EOperationType.Insert, captured!.TypeOperation);
        Assert.Equal(EOperationStatus.Pending, captured.Status);
        Assert.Equal("TUser", captured.EntityName);
        Assert.Equal("Id", captured.EntityKey);
    }

    [Fact]
    public void TrackUpdate_AndTrackDelete_AlsoWork()
    {
        var sut = new TrackChanges();

        sut.TrackUpdate(new { X = 1 }, "TUser", "Id");
        sut.TrackDelete(new { X = 1 }, "TUser", "Id");

        Assert.Equal(2, sut.Operations.Count);
        Assert.Equal(EOperationType.Update, sut.Operations[0].TypeOperation);
        Assert.Equal(EOperationType.Delete, sut.Operations[1].TypeOperation);
    }

    [Theory]
    [InlineData(null, "T", "K")]
    public void TrackInsert_NullValue_Throws(object? value, string table, string key)
    {
        var sut = new TrackChanges();
        Assert.Throws<ArgumentNullException>(() => sut.TrackInsert(value!, table, key));
    }

    [Theory]
    [InlineData("", "Id")]
    [InlineData("   ", "Id")]
    [InlineData("Table", "")]
    [InlineData("Table", "  ")]
    public void TrackInsert_EmptyMetadata_Throws(string table, string key)
    {
        var sut = new TrackChanges();
        Assert.Throws<ArgumentException>(() => sut.TrackInsert(new { X = 1 }, table, key));
    }

    [Fact]
    public void MarkCommitted_ChangesOnlyPendingOperations()
    {
        var sut = new TrackChanges();
        sut.TrackInsert(new { }, "T", "Id");
        sut.TrackUpdate(new { }, "T", "Id");

        // Один уже отметим как Failed — он не должен переключиться на Committed.
        sut.SetStatus(sut.Operations[0].Id, EOperationStatus.Failed);

        sut.MarkCommitted();

        Assert.Equal(EOperationStatus.Failed, sut.Operations[0].Status);
        Assert.Equal(EOperationStatus.Committed, sut.Operations[1].Status);
    }

    [Fact]
    public void MarkFailed_FlipsAllPendingToFailed()
    {
        var sut = new TrackChanges();
        sut.TrackInsert(new { }, "T", "Id");
        sut.TrackInsert(new { }, "T", "Id");

        sut.MarkFailed();

        Assert.All(sut.Operations, op => Assert.Equal(EOperationStatus.Failed, op.Status));
    }

    [Fact]
    public void SetStatus_UnknownId_Throws()
    {
        var sut = new TrackChanges();
        Assert.Throws<InvalidOperationException>(
            () => sut.SetStatus(Guid.NewGuid(), EOperationStatus.Committed));
    }
}
