using DORM.Mapping.Reflect;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DORM.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultSqlAttribute : System.Attribute, IApplyAttribute
    {
        public string SqlQuery { get;  }

        public DefaultSqlAttribute(string sqlQuery)
        {
            if (!Regex.IsMatch(sqlQuery, @"^[a-zA-Z0-9()._ ]+$"))
            {
                throw new ArgumentException("Incorrect command on Default");
            }
            SqlQuery = sqlQuery;
        }

        public void Apply(TableField tField, PropertyInfo info)
        {
            tField.DefaultSqlValue = SqlQuery;
        }
    }
}
