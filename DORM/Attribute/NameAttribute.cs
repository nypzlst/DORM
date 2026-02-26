using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Attribute
{

    /// <summary>
    /// Атрибут який надає пріорітет назві колонки, таблиці
    /// </summary> 
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Property , AllowMultiple = false)]
    public class NameAttribute : System.Attribute
    {
        string Name;

        public NameAttribute(string name)
        {
            Name = name;
        }

        public string GetName() => Name;
    }
}
