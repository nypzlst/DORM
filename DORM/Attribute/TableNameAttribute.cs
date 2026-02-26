using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Attribute
{

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class TableNameAttribute : System.Attribute
    {
        public string Name;

        public TableNameAttribute(string name)
        {
            Name = name;
        }

        public string GetName() => Name;
    }
}
