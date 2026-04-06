using DORM.Attribute;
using DORM.Infrastructure.Cache;
using DORM.Infrastructure.Core;
using DORM.Mapping;
using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DORM.Providers.MySQL
{
    internal class AdditionalQueryMethod
    {
        internal static string GetNameTable<T>()
        {
            Type type = typeof(T);
            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = UniversalMethod.SanitizeName(AttributeNameTable?.Name ?? type.Name);
            return NameTable;
        }

        internal static List<TableField> GetCachedTable<T>(string tableName, ICacheController _cache) where T : class
        {
            if (_cache.TryGet(tableName, out List<TableField> table))
                return table;

            var fileds = MappingClass.MapClass<T>();
            _cache.Store(tableName, fileds);
            return fileds;
        }

        internal static (string idFieldName, object idFieldValue) GetPrimaryKey<T>(List<TableField> table, T entity)
        {
            Type type = typeof(T);

            var pkField = table.SingleOrDefault(x => x.IsPrimaryKey);
            if (pkField is null)
                throw new ArgumentException("Primary key field not found");

            var props = type.GetProperties();
            var pkProperty = props.SingleOrDefault(p =>
            {
                var nameAttr = p.GetCustomAttribute<NameAttribute>();
                var column = UniversalMethod.SanitizeName(nameAttr?.Name ?? p.Name);
                return column == pkField.FieldName;
            });
            if (pkProperty is null)
                throw new ArgumentException("Primary key property not found");
            var IdPropertyVal = pkProperty.GetValue(entity);
            if (IdPropertyVal is null)
                throw new ArgumentException("Primary key value cannot be null");

            return (pkField.FieldName, IdPropertyVal);
        }

        internal static void BuildWhereById(StringBuilder sb,string idField, object idValue)
        {
            sb.Append(" WHERE ").Append(idField).Append(" = @").Append(idField);
        }

    }
}
