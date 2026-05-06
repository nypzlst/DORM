using System.Linq.Expressions;
using DORM.Infrastructure.Cache;
using DORM.Providers.MySQL;
using DORM.Tests.Fixtures;

namespace DORM.Tests;

public class MySqlCrudQueryTests
{
    private static MySqlCrudQuery NewCrud()
        => new MySqlCrudQuery { Cache = new MemoryCacheController() };

    // ─── CREATE TABLE ────────────────────────────────────────────────────────

    [Fact]
    public void CreateTable_User_UsesNameAttribute_AndPkColumn()
    {
        var sql = NewCrud().CreateTable(new UserModel());

        Assert.StartsWith("CREATE TABLE TUser(", sql);
        Assert.Contains("Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY", sql);
        Assert.Contains("Email VARCHAR(255) UNIQUE", sql);
        Assert.EndsWith(");", sql);
    }

    [Fact]
    public void CreateTable_Catalog_GeneratesForeignKey_PointingToReferencedTableName()
    {
        var crud = NewCrud();
        // Прогреем кэш родительской таблицы — это в реальном коде делает CreateTable<UserModel>.
        crud.CreateTable(new UserModel());
        var sql = crud.CreateTable(new CatalogModel());

        Assert.Contains("FOREIGN KEY (IdUser) REFERENCES TUser(Id)", sql);
        // Дефолт для строки должен быть в кавычках.
        Assert.Contains("DEFAULT 'Template'", sql);
    }

    [Fact]
    public void CreateTable_PlainEntity_TableNameEqualsClassName()
    {
        var sql = NewCrud().CreateTable(new PlainEntity());
        Assert.StartsWith("CREATE TABLE PlainEntity(", sql);
    }

    // ─── INSERT ──────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_User_ExcludesPrimaryKey_AndUsesColumnName()
    {
        var crud = NewCrud();
        var pq = crud.Insert(new UserModel { Email = "test@example.com" });

        Assert.Contains("INSERT INTO TUser", pq.Sql);
        Assert.Contains("(Email)", pq.Sql);
        Assert.DoesNotContain("Id", pq.Sql.Substring(0, pq.Sql.IndexOf("VALUES")));
        Assert.True(pq.Parameters.ContainsKey("@Email"));
        Assert.Equal("test@example.com", pq.Parameters["@Email"]);
    }

    [Fact]
    public void Insert_RenamedModel_UsesColumnName_NotPropertyName()
    {
        // Регрессионный тест на ранее найденный баг: Insert использовал info.Name,
        // и при [Name("EmailAddress")] на свойстве Email генерировал неверный SQL.
        var crud = NewCrud();
        var pq = crud.Insert(new RenamedModel { Email = "x@y.z" });

        Assert.Contains("(EmailAddress)", pq.Sql);
        Assert.DoesNotContain("(Email)", pq.Sql);
        Assert.True(pq.Parameters.ContainsKey("@EmailAddress"));
        Assert.False(pq.Parameters.ContainsKey("@Email"));
    }

    [Fact]
    public void Insert_NullValue_IsConvertedToDBNull()
    {
        var crud = NewCrud();
        var pq = crud.Insert(new UserModel { Email = null });

        Assert.Equal(DBNull.Value, pq.Parameters["@Email"]);
    }

    // ─── UPDATE ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_User_BuildsSetAndWhere_AndExcludesPk()
    {
        var crud = NewCrud();
        var pq = crud.Update(new UserModel { Id = 7, Email = "u@e.com" });

        Assert.Contains("UPDATE TUser SET", pq.Sql);
        Assert.Contains("Email = @Email", pq.Sql);
        Assert.DoesNotContain("Id = @Id,", pq.Sql); // PK не в SET
        Assert.Contains("WHERE Id = @Id", pq.Sql);

        Assert.Equal("u@e.com", pq.Parameters["@Email"]);
        Assert.Equal(7, pq.Parameters["@Id"]);
    }

    [Fact]
    public void Update_NullPk_Throws()
    {
        var crud = NewCrud();
        // Id это int, в null его не положишь, поэтому проверим на свежей модели,
        // где PK можно занулить.
        Assert.Throws<ArgumentException>(() => crud.Update(new NullablePkModel()));
    }

    // ─── DELETE ──────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_User_BuildsExpectedSqlAndParam()
    {
        var crud = NewCrud();
        var pq = crud.Delete(new UserModel { Id = 11 });

        Assert.Equal("DELETE FROM TUser WHERE Id = @Id", pq.Sql);
        Assert.Single(pq.Parameters);
        Assert.Equal(11, pq.Parameters["@Id"]);
    }

    // ─── SELECT ──────────────────────────────────────────────────────────────

    [Fact]
    public void Select_MemberExpression_GeneratesSingleAliasedColumn()
    {
        var crud = NewCrud();
        crud.CreateTable(new UserModel()); // прогреваем кэш

        var sql = crud.Select<UserModel, string>(u => u.Email!);

        Assert.Equal("SELECT Email AS Email FROM TUser", sql);
    }

    [Fact]
    public void Select_AnonymousProjection_BuildsAliasedColumns()
    {
        var crud = NewCrud();
        crud.CreateTable(new UserModel());

        var sql = SelectAnon(crud, (UserModel u) => new { u.Id, u.Email });

        Assert.Equal("SELECT Id AS Id, Email AS Email FROM TUser", sql);
    }

    [Fact]
    public void Select_UnsupportedExpression_Throws()
    {
        var crud = NewCrud();
        crud.CreateTable(new UserModel());

        // Тело-выражение — BinaryExpression, не NewExpression и не MemberExpression.
        Assert.Throws<ArgumentException>(
            () => crud.Select<UserModel, string>(u => u.Email + "_x"));
    }

    /// <summary>
    /// Хелпер для вывода типа анонимной проекции — generic'и выводятся из лямбды.
    /// </summary>
    private static string SelectAnon<T, TR>(MySqlCrudQuery crud, Expression<Func<T, TR>> selector)
        where T : class
        => crud.Select<T, TR>(selector);

    // ─── вспомогательные модели ─────────────────────────────────────────────

    public class NullablePkModel
    {
        [DORM.Attribute.PK]
        public int? Id { get; set; }

        public string? Title { get; set; }
    }
}
