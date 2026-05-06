using DORM.Attribute;

namespace DORM.Tests.Fixtures;

[Name("TUser")]
public class UserModel
{
    [PK]
    public int Id { get; set; }

    [Unique]
    public string? Email { get; set; }
}

public class CatalogModel
{
    [PK]
    public int Id { get; set; }

    [Default("Template"), Unique]
    public string? Name { get; set; }

    [FK(typeof(UserModel), nameof(UserModel.Id))]
    public int IdUser { get; set; }
}

/// <summary>
/// Модель для проверки работы [Name] на свойстве — колонка должна называться "EmailAddress",
/// а property — "Email". Покрывает фикс бага в MySqlCrudQuery.Insert.
/// </summary>
[Name("TRenamed")]
public class RenamedModel
{
    [PK]
    public int Id { get; set; }

    [Name("EmailAddress"), Unique]
    public string? Email { get; set; }
}

public class WithDefaultsModel
{
    [PK]
    public int Id { get; set; }

    [Default("Anonymous")]
    public string? Name { get; set; }

    [Default(true)]
    public bool IsActive { get; set; }

    [Default(42)]
    public int Score { get; set; }

    [DefaultSql("CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>Без [Name] — таблица должна называться так же, как класс.</summary>
public class PlainEntity
{
    [PK]
    public int Id { get; set; }
    public string? Title { get; set; }
}
