namespace DORM.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultAttribute : System.Attribute
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

    }
}
