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
        public bool isNullability { get; internal set; }
        public bool isUnique { get; internal set; } = false;
        public bool isPrimaryKey { get; internal set; } = false;
        public bool isForeginKey { get; internal set; } = false;

        public object DefaultValue { get; internal set; }
        public string DefaultSqlValue { get; internal set; }  

        public Type FKReferenceTable { get; internal set; }
        public string FKReferenceId{ get; internal set; }

        public TableField(string fieldName, string fieldType, bool isNullability = true)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            this.isNullability = isNullability;
        }

        public override string ToString()
        {
            if (!Regex.IsMatch(FieldName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return "ErrorColumnName";
            }
            if (!TypeMap.TryGetValue(FieldType, out var sqlType))
            {
                return "TypeValueNotCorrect";
            }


            var temp = TypeMap.GetValueOrDefault(FieldType);
            string _requestString = $"{FieldName} {temp.ToUpper()}";
            if (isNullability && !isPrimaryKey)
            {
                _requestString += " NOT NULL";
            }
            if (isUnique)
            {
                _requestString += " UNIQUE";
            }
            if (isPrimaryKey)
            {
                _requestString += " NOT NULL AUTO_INCREMENT PRIMARY KEY ";
            }
            //  написать фк в другом метсе
            //if (isForeginKey)
            //{
            //    _requestString += $"FOREIGN KEY ({field.FieldName}) REFERENCES {field.FKReferenceTable.Name}({field.FKReferenceId})"           
            //}
            if (DefaultValue != null)
            {
                _requestString += $" DEFAULT {DefaultValue}";
            }
            else if (DefaultSqlValue != null)
            {
                _requestString += $" DEFAULT ({DefaultSqlValue})";
            }

            return _requestString;

        }

        private static readonly Dictionary<string, string> TypeMap = new()
        {
            { "String",   "nvarchar(255)" },
            { "Int32",    "int"           },
            { "Int64",    "bigint"        },
            { "Boolean",  "bit"           },
            { "DateTime", "datetime"      },
            { "Decimal",  "decimal(18,2)" },
            { "Double",   "float"         },
        };

    }
}
