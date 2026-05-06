using DORM.Mapping.Reflect;

namespace DORM.Tests;

public class TableFieldTests
{
    [Fact]
    public void ToString_StringNotNullable()
    {
        var f = new TableField("Email", "String", isNullability: false);
        Assert.Equal("Email VARCHAR(255) NOT NULL", f.ToString());
    }

    [Fact]
    public void ToString_StringNullable()
    {
        var f = new TableField("Email", "String", isNullability: true);
        Assert.Equal("Email VARCHAR(255)", f.ToString());
    }

    [Fact]
    public void ToString_PrimaryKey_HasAutoIncrement_NoNotNull()
    {
        var f = new TableField("Id", "Int32", isNullability: false) { IsPrimaryKey = true };
        // PK сам по себе содержит NOT NULL, и ветка с обычным NOT NULL не должна срабатывать.
        var sql = f.ToString();
        Assert.Contains("NOT NULL AUTO_INCREMENT PRIMARY KEY", sql);
        // Не должно быть «двойного» NOT NULL.
        Assert.Equal(1, CountOccurrences(sql, "NOT NULL"));
    }

    [Fact]
    public void ToString_Unique()
    {
        var f = new TableField("Email", "String") { IsUnique = true };
        Assert.Contains("UNIQUE", f.ToString());
    }

    [Fact]
    public void ToString_DefaultString_IsQuotedAndEscaped()
    {
        var f = new TableField("Name", "String") { DefaultValue = "O'Brien" };
        Assert.Contains("DEFAULT 'O''Brien'", f.ToString());
    }

    [Fact]
    public void ToString_DefaultBool_IsZeroOrOne()
    {
        var fTrue = new TableField("IsActive", "Boolean") { DefaultValue = true };
        var fFalse = new TableField("IsActive", "Boolean") { DefaultValue = false };
        Assert.Contains("DEFAULT 1", fTrue.ToString());
        Assert.Contains("DEFAULT 0", fFalse.ToString());
    }

    [Fact]
    public void ToString_DefaultDateTime_IsFormatted()
    {
        var dt = new DateTime(2024, 1, 2, 3, 4, 5);
        var f = new TableField("CreatedAt", "DateTime") { DefaultValue = dt };
        Assert.Contains("DEFAULT '2024-01-02 03:04:05'", f.ToString());
    }

    [Fact]
    public void ToString_DefaultSql_IsParenthesized()
    {
        var f = new TableField("CreatedAt", "DateTime") { DefaultSqlValue = "CURRENT_TIMESTAMP" };
        Assert.Contains("DEFAULT (CURRENT_TIMESTAMP)", f.ToString());
    }

    [Fact]
    public void ToString_UnknownType_Throws()
    {
        var f = new TableField("Foo", "Guid");
        Assert.Throws<ArgumentException>(() => f.ToString());
    }

    [Fact]
    public void Constructor_InvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TableField("1Bad", "String"));
    }

    private static int CountOccurrences(string source, string substr)
    {
        int count = 0, idx = 0;
        while ((idx = source.IndexOf(substr, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += substr.Length;
        }
        return count;
    }
}
