using DORM.Attribute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DORM.Mapping
{

    /// <summary>
    /// Клас котрий відповідає за рефлексію пов'язаною з таблицями бази даних
    /// </summary>

    public class TableReflect
    {
        /// <summary>
        /// Метод для створення таблиці, котрий отримує клас користувача
        /// </summary>
        /// <param name="obj">Клас котрий містить в собі параметри</param>
        public string CreateTable<T>() where T : class
        {
            var sb = new StringBuilder();
            Type type = typeof(T);
            List<TableField> table = new List<TableField>();

            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = SanitizeName(AttributeNameTable?.Name ?? type.Name);


            sb.Append("CREATE TABLE ")
              .Append(NameTable)
              .Append("(");


            foreach(PropertyInfo info in type.GetProperties())
            {
                var pName = info.Name;
                var pType = info.PropertyType.Name;
                var pNullAllowed = CheckNullInProperty(info);              
                TableField tField = new TableField(info.Name,pType,pNullAllowed);
                CheckAttributes(info, tField);
                    
                table.Add(tField);
            }

            sb.Append(string.Join(", ", table));

            var foreignKeys = table
                .Where(f => f.IsForeignKey)
                .Select(f =>
                {
                    var refNameAttr = (NameAttribute)System.Attribute.GetCustomAttribute(f.FKReferenceTable, typeof(NameAttribute));
                    string refTableName = refNameAttr?.Name ?? f.FKReferenceTable.Name;
                    return $"FOREIGN KEY ({f.FieldName}) REFERENCES {refTableName}({f.FKReferenceId})";
                });

            if (foreignKeys.Any())
            {
                sb.Append(", ").Append(string.Join(", ", foreignKeys));
            }
            sb.Append(");");
            return sb.ToString();

        }

        private static bool CheckNullInProperty(PropertyInfo info)
        {
            var context = new NullabilityInfoContext();
            var check = context.Create(info);
            if (check.WriteState == NullabilityState.Nullable)
            {
                return true;
            }
            return false;
        }


        private static void CheckAttributes(PropertyInfo info, TableField tField)
        {
            System.Attribute[] attributes = System.Attribute.GetCustomAttributes(info);
            foreach (var attr in attributes)
            {
                if (attr is IApplyAttribute applyAttribute)
                {
                    applyAttribute.Apply(tField, info);
                }
            }
        }


        internal static string SanitizeName(string name)
        {
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new ArgumentException($"Invalid table name: {name}");
            return name;
        }
    }



}
