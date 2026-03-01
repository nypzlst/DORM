using DORM.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Attribute
{

    [System.AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : System.Attribute, IApplyAttribute
    {
        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.isUnique = true;
        }
    }
}
