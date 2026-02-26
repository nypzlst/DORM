using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultAttribute : System.Attribute
    {
        Object DefaultValue;

        DefaultAttribute(object value)
        {
            DefaultValue = value;
        }

        public object GetDefaultValue() => DefaultValue;

        
    }
}
