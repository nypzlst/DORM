using DORM.Exceptions;
using DORM.Infrastructure.Core;

namespace DORM.Tests;

public class DatabaseTests
{
    [Fact]
    public void Constructor_BadFilePath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Database("Z:/no/such/file.txt"));
    }

    [Fact]
    public async Task CheckConnection_UnreachableServer_ThrowsDormExecutionException()
    {
        // Несуществующий хост — драйвер быстро упадёт.
        var db = new Database(
            namedb: "no_db",
            server: "127.0.0.1",
            user: "u",
            password: "p",
            port: 1); // явно «никем не слушаемый» порт

        await Assert.ThrowsAsync<DormExecutionException>(() => db.CheckConnection());
    }

    [Fact]
    public void ConstructConnectionString_ContainsAllParts()
    {
        var db = new Database("name", "srv", "u", "pw", 3307);

        // constructConnectionString — internal, доступен через InternalsVisibleTo.
        var conn = db.constructConnectionString();

        Assert.Contains("Server=srv", conn);
        Assert.Contains("Database=name", conn);
        Assert.Contains("User ID=u", conn);
        Assert.Contains("Password=pw", conn);
        Assert.Contains("Port=3307", conn);
    }
}
