using DORM.Infrastructure.Core;

namespace DORM.Tests;

public class UniversalMethodTests
{
    [Theory]
    [InlineData("Users")]
    [InlineData("_Users")]   // SanitizeName допускает начало с подчёркивания
    [InlineData("TUser_2")]
    public void SanitizeName_AcceptsValidNames(string name)
    {
        var result = UniversalMethod.SanitizeName(name);
        Assert.Equal(name, result);
    }

    [Theory]
    [InlineData("1User")]
    [InlineData("User; DROP")]
    [InlineData("user-name")]
    [InlineData("")]
    [InlineData(" ")]
    public void SanitizeName_RejectsInvalidNames(string name)
    {
        Assert.Throws<ArgumentException>(() => UniversalMethod.SanitizeName(name));
    }
}
