using DORM.Mapping;
using System.Reflection;

namespace DORM.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultAttribute : System.Attribute, IApplyAttribute
    {
        public Object DefaultValue { get; }

        public DefaultAttribute(Object value)
        {
            DefaultValue = value;
        }

        public DefaultAttribute(string value)
        {
            DefaultValue = $"'{value}'";
        }

        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.DefaultValue = DefaultValue;
        }
    }
}
