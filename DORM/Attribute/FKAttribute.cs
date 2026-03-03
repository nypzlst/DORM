using DORM.Mapping.Reflect;
using System.Reflection;

namespace DORM.Attribute
{

    [AttributeUsage(AttributeTargets.Property)]
    public class FKAttribute : System.Attribute, IApplyAttribute
    {
// доробити зв'язки many-to-many, one-to-many ...

        public Type ReferenceTable { get; }
        public string ReferenceTableId { get; }

        public FKAttribute(Type table, string idTable)
        {
            ReferenceTable = table ?? throw new ArgumentNullException(nameof(table));
            ReferenceTableId = string.IsNullOrEmpty(idTable) ? throw new ArgumentException("Null Id Field") : idTable;
        }

        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.IsForeignKey = true;
            tField.FKReferenceId = ReferenceTableId;
            tField.FKReferenceTable = ReferenceTable;
        }
    }
}
