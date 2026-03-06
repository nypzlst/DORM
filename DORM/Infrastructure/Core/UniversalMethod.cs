using System.Text.RegularExpressions;

namespace DORM.Infrastructure.Core
{
    public class UniversalMethod
    {
        internal static string SanitizeName(string name)
        {
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new ArgumentException($"Invalid table name: {name}");
            return name;
        }
    }
}