using DORM.Attribute;

namespace TestORM.Example
{
    [Name("TUser")]
    public class User
    {
        [PK]
        public int Id { get; set; }

        [Unique]
        public string? Email { get; set; }
    }
}
