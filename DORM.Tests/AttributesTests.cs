using DORM.Attribute;

namespace DORM.Tests;

public class AttributesTests
{
    [Theory]
    [InlineData("Users")]
    [InlineData("TUser")]
    [InlineData("Some_Table_2")]
    public void NameAttribute_AcceptsValidNames(string name)
    {
        var attr = new NameAttribute(name);
        Assert.Equal(name, attr.Name);
    }

    [Theory]
    [InlineData("1User")]      // не может начинаться с цифры
    [InlineData("_User")]      // регекс требует букву в начале
    [InlineData("User; DROP")] // запрещённые символы
    [InlineData("")]
    public void NameAttribute_RejectsInvalidNames(string name)
    {
        Assert.Throws<ArgumentException>(() => new NameAttribute(name));
    }

    [Theory]
    [InlineData("CURRENT_TIMESTAMP")]
    [InlineData("NOW()")]
    [InlineData("UUID()")]
    [InlineData("LOWER(name)")]
    public void DefaultSqlAttribute_AcceptsValidExpressions(string sql)
    {
        var attr = new DefaultSqlAttribute(sql);
        Assert.Equal(sql, attr.SqlQuery);
    }

    [Theory]
    [InlineData("DROP TABLE users;")]
    [InlineData("'admin'")]
    [InlineData("a-b")]
    public void DefaultSqlAttribute_RejectsBadExpressions(string sql)
    {
        Assert.Throws<ArgumentException>(() => new DefaultSqlAttribute(sql));
    }

    [Fact]
    public void FKAttribute_NullTable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FKAttribute(null!, "Id"));
    }

    [Fact]
    public void FKAttribute_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FKAttribute(typeof(string), ""));
    }
}
