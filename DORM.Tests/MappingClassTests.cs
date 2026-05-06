using DORM.Mapping;
using DORM.Tests.Fixtures;

namespace DORM.Tests;

public class MappingClassTests
{
    [Fact]
    public void MapClass_User_HasTwoFields_PkAndUnique()
    {
        var fields = MappingClass.MapClass<UserModel>();

        Assert.Equal(2, fields.Count);

        var id = fields.Single(f => f.FieldName == "Id");
        Assert.True(id.IsPrimaryKey);
        Assert.False(id.IsUnique);
        Assert.False(id.IsForeignKey);

        var email = fields.Single(f => f.FieldName == "Email");
        Assert.False(email.IsPrimaryKey);
        Assert.True(email.IsUnique);
        Assert.True(email.IsNullable, "string? должно дать IsNullable=true");
    }

    [Fact]
    public void MapClass_Catalog_HasForeignKey_PointingToUserModel()
    {
        var fields = MappingClass.MapClass<CatalogModel>();

        var fk = fields.Single(f => f.IsForeignKey);
        Assert.Equal("IdUser", fk.FieldName);
        Assert.Equal(typeof(UserModel), fk.FKReferenceTable);
        Assert.Equal("Id", fk.FKReferenceId);
    }

    [Fact]
    public void MapClass_Catalog_NameField_HasDefaultAndUnique()
    {
        var fields = MappingClass.MapClass<CatalogModel>();
        var name = fields.Single(f => f.FieldName == "Name");

        Assert.True(name.IsUnique);
        Assert.Equal("Template", name.DefaultValue);
    }

    [Fact]
    public void MapClass_Renamed_NameAttributeOnProperty_OverridesFieldName()
    {
        var fields = MappingClass.MapClass<RenamedModel>();
        // Свойство Email с [Name("EmailAddress")] должно дать колонку EmailAddress.
        Assert.Contains(fields, f => f.FieldName == "EmailAddress");
        Assert.DoesNotContain(fields, f => f.FieldName == "Email");
    }

    [Fact]
    public void MapClass_WithDefaults_AllDefaultsPicked()
    {
        var fields = MappingClass.MapClass<WithDefaultsModel>();

        Assert.Equal("Anonymous", fields.Single(f => f.FieldName == "Name").DefaultValue);
        Assert.Equal(true, fields.Single(f => f.FieldName == "IsActive").DefaultValue);
        Assert.Equal(42, fields.Single(f => f.FieldName == "Score").DefaultValue);
        Assert.Equal("CURRENT_TIMESTAMP",
            fields.Single(f => f.FieldName == "CreatedAt").DefaultSqlValue);
    }
}
