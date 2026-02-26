using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Attribute
{

    [AttributeUsage(AttributeTargets.Property)]
    public class FKAttribute : System.Attribute
    {

        public Type ReferenceTable { get; }
        public string ReferenceTableId { get; }

        public FKAttribute(Type Table, string IdTable)
        {
            ReferenceTable = Table ?? throw new ArgumentNullException(nameof(Table));
            ReferenceTableId = string.IsNullOrEmpty(IdTable) ? throw new ArgumentException("Null Id Field") : IdTable;
        }

        
    }
}
