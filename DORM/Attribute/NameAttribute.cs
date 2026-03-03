using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DORM.Attribute
{

    /// <summary>
    /// Атрибут який надає пріорітет назві колонки, таблиці
    /// </summary> 
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Property , AllowMultiple = false)]
    public class NameAttribute : System.Attribute, IApplyAttribute
    {
        public string Name { get; }

        public NameAttribute(string name)
        {
            if (!Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException("Invalid name format.");
            }
            Name = name;
        }

        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.FieldName = info.Name ?? Name;
        }
    }
}
