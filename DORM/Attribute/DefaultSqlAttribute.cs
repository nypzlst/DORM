using System.Text.RegularExpressions;

namespace DORM.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultSqlAttribute : System.Attribute
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
    }
}
