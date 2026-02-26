namespace DORM.Attribute
{

    [AttributeUsage(AttributeTargets.Property)]
    public class FKAttribute : System.Attribute
    {

        public Type ReferenceTable { get; }
        public string ReferenceTableId { get; }

        public FKAttribute(Type table, string idTable)
        {
            ReferenceTable = table ?? throw new ArgumentNullException(nameof(table));
            ReferenceTableId = string.IsNullOrEmpty(idTable) ? throw new ArgumentException("Null Id Field") : idTable;
        }

        
    }
}
