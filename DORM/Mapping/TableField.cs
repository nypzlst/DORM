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
        public string FieldName { get; set; } 
        public string FieldType { get; set; }
        public bool isNullability { get; set; } = true;
        public bool isUnique { get; set; } = false;
        public bool isPrimaryKey { get; set; } = false;
        public bool isForeginKey { get; set; } = false;





        /// <summary>
        /// Конструктор який приймає атрибути полів
        /// </summary>
        /// <param name="fieldName">Назва колонки</param>
        /// <param name="fieldType">Тип колонки</param>
        /// <param name="nullability">Можливість бути null</param>
        public TableField(string fieldName, string fieldType, bool nullability = true, bool unique = false, bool pk = false, bool fk = false )
        {
            FieldName = fieldName;
            FieldType = fieldType;
            isNullability = nullability;
            isUnique = unique;
            isPrimaryKey = pk;
            isForeginKey = fk;
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
            if (!isNullability)
            {
                _requestString += " NOT NULL";
            }
            if (isUnique)
            {
                _requestString += " AUTO_INCREMENT";
            }
            if (isPrimaryKey)
            {
                _requestString += " PRIMARY KEY ";
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
