using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DORM.Mapping
{
    public class TableField
    {
        public string FieldName { get; set; } 
        public string FieldType { get; set; }
        public bool Nullability { get; set; }

        public TableField(string fieldName, string fieldType, bool nullability)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            Nullability = nullability;
        }


        public override string ToString()
        {
            if (!Regex.IsMatch(FieldName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException("Invalid Column Name");
            }
            if (!TypeMap.TryGetValue(FieldType, out var sqlType))
            {
                throw new NotSupportedException($"Type {FieldType} is not supported");
            }


            var temp = TypeMap.GetValueOrDefault(FieldType);
            string _requestString;
            if (Nullability)
            {
                _requestString = $"{FieldName} {temp} ";
            }
            else
            {
                _requestString = $"{FieldName} {temp} NOT NULL";
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
