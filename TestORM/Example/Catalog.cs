using DORM.Attribute;

namespace TestORM.Example
{
    public class Catalog
    {
        [PK]
        public int Id { get; set; }

        [Default("Template"), Unique]
        public string? Name { get; set; }

        [FK(typeof(User), nameof(User.Id))]
        public int IdUser { get; set; }
    }
}
