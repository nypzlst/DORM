using DORM.Attribute;
using DORM.Infrastructure.Cache;
using DORM.Infrastructure.Core;
using DORM.Infrastructure.CRUD;
using DORM.Mapping;
using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DORM.Providers.MySQL{

    public class MySqlCrudQuery<T> : ICrudQuery<T> where T : class
    {
        private readonly ICacheController _cache;

        public MySqlCrudQuery(ICacheController cache)
        {
            _cache = cache;
        }

        public string Create(T entity) 
        {
            var sb = new StringBuilder();
            Type type = typeof(T);

            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = UniversalMethod.SanitizeName(AttributeNameTable?.Name ?? type.Name);

            List<TableField> table = MappingClass.MapClass<T>();
            _cache.Store(NameTable, table);


            sb.Append("CREATE TABLE ")
              .Append(NameTable)
              .Append("( ")
              .Append(string.Join(", ", table));


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

        public string Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            Type type = typeof(T);
            string NameTable = type.Name;
            List<TableField> table;

            if (!_cache.TryGet(NameTable, out table))
                throw new ArgumentException("Don`t cached table");
                // дописати виклик кешування таблиці якщо її не має в кеші

            var body = (NewExpression)selector.Body;
            var reqFields = body.Members.Select(m => m.Name).ToList();

            var fields = reqFields.Where(x => table.Any(t => t.FieldName == x));

            if (!fields.Any())
                throw new ArgumentException("No one fields find");


            return $"SELECT {string.Join(", ", fields)} FROM {NameTable}";
        }

        public string Update(T entity)
        {
            throw new NotImplementedException();
        }

        public string Delete(T entity)
        {
            throw new NotImplementedException();
        }
    }



}
