using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DORM.Mapping
{

    /// <summary>
    /// Клас прокладака для створення полів в таблиці
    /// </summary>
    public class TableField
    {
        public string FieldName { get; internal set; } 
        public string FieldType { get; internal set; }
        public bool IsNullable { get; internal set; }
        public bool IsUnique { get; internal set; } = false;
        public bool IsPrimaryKey { get; internal set; } = false;
        public bool IsForeignKey { get; internal set; } = false;

        public object DefaultValue { get; internal set; }
        public string DefaultSqlValue { get; internal set; }  

        public Type FKReferenceTable { get; internal set; }
        public string FKReferenceId{ get; internal set; }

        public TableField(string fieldName, string fieldType, bool isNullability = true)
        {
            FieldName = TableReflect.SanitizeName(fieldName);
            FieldType = fieldType;
            IsNullable = isNullability;
        }

        public override string ToString()
        {
            //if (!Regex.IsMatch(FieldName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            //{
            //    return "ErrorColumnName";
            //}
            


            var temp = TypeMap.GetValueOrDefault(FieldType) ?? throw new ArgumentException($"Incorect type : {FieldType}");
            string requestString = $"{FieldName} {temp.ToUpper()}";

            if (!IsNullable && !IsPrimaryKey)
            {
                requestString += " NOT NULL";
            }
            if (IsUnique)
            {
                requestString += " UNIQUE";
            }
            if (IsPrimaryKey)
            {
                requestString += " NOT NULL AUTO_INCREMENT PRIMARY KEY ";
            }
            if (DefaultValue != null)
            {
                requestString += $" DEFAULT {DefaultValue}";
            }
            else if (DefaultSqlValue != null)
            {
                requestString += $" DEFAULT ({DefaultSqlValue})";
            }

            return requestString;

        }

        private static readonly Dictionary<string, string> TypeMap = new()
        {
            { "String",   "varchar(255)"  },
            { "Int32",    "int"           },
            { "Int64",    "bigint"        },
            { "Boolean",  "tinyint(1)"    },
            { "DateTime", "datetime"      },
            { "Decimal",  "decimal(18,2)" },
            { "Double",   "double"        },
        };

    }
}
