using DORM.Infrastructure.Cache;
using DORM.Mapping.Reflect;

namespace DORM.Tests;

public class MemoryCacheControllerTests
{
    [Fact]
    public void Store_Then_TryGet_RoundTrip()
    {
        var sut = new MemoryCacheController();
        var fields = new List<TableField> { new TableField("Id", "Int32") };

        sut.Store("TUser", fields);

        Assert.True(sut.TryGet("TUser", out var stored));
        Assert.Same(fields, stored);
    }

    [Fact]
    public void TryGet_Missing_ReturnsFalse()
    {
        var sut = new MemoryCacheController();

        Assert.False(sut.TryGet("Nope", out var stored));
        Assert.Null(stored);
    }

    [Fact]
    public void Store_Twice_OverwritesEntry()
    {
        var sut = new MemoryCacheController();
        var first = new List<TableField> { new TableField("A", "Int32") };
        var second = new List<TableField> { new TableField("B", "String") };

        sut.Store("T", first);
        sut.Store("T", second);

        Assert.True(sut.TryGet("T", out var stored));
        Assert.Same(second, stored);
    }
}
