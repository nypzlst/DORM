using DORM.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Attribute
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PKAttribute : System.Attribute, IApplyAttribute
    {
        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.isPrimaryKey = true;
        }
    }
}
