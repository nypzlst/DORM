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
using System.Reflection.Emit;
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

            IEnumerable<string> reqFields;

            if(selector.Body is NewExpression body)
                reqFields = body.Members.Select(m => m.Name);
            else if(selector.Body is MemberExpression member)
                reqFields = new[] { member.Member.Name };
            else
                throw new ArgumentException("Incorect anonyms type");
            
            var fields = reqFields.Where(x => table.Any(t => t.FieldName == x));

            if (!fields.Any())
                throw new ArgumentException("No one fields find");

            return $"SELECT {string.Join(", ", fields)} FROM {NameTable}";
            // Expression Visitor для селекта з where
        }

        public string Update(T entity)
        {
            Type type = typeof(T);
            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = UniversalMethod.SanitizeName(AttributeNameTable?.Name ?? type.Name);

            List<TableField> table;
            if (!_cache.TryGet(NameTable, out table))
                throw new ArgumentException("Don`t cached table");

            string IdField = table.Single(x => x.IsPrimaryKey).FieldName;
            var IdPropertyVal = type.GetProperty(IdField).GetValue(entity);
            var sb = new StringBuilder();


            var changesList = new List<string>();

            sb.Append("UPDATE ").Append(NameTable).Append(" SET ");
            foreach(PropertyInfo info in type.GetProperties())
            {
                
                var field = table.SingleOrDefault(x => x.FieldName == info.Name);
                var fieldValue = info.GetValue(entity);

                if (field is not null && fieldValue is not null )
                {
                    if (field.IsPrimaryKey) continue; 

                    if (fieldValue is string || fieldValue is DateTime)
                        changesList.Add($"{field.FieldName} = '{fieldValue}'"); 
                    else if (fieldValue is bool b)
                        changesList.Add($"{field.FieldName} = {(b ? 1 : 0)}");
                    else
                        changesList.Add($"{field.FieldName} = {fieldValue}");
                }
            }

            sb.Append(string.Join(", ", changesList));
            
            
            if (IdPropertyVal is int)
                sb.Append(" WHERE ").Append(IdField).Append(" = ").Append(IdPropertyVal);
            else if (IdPropertyVal is Guid)
                sb.Append(" WHERE ").Append(IdField).Append(" = ").Append($"'{IdPropertyVal}'");
            else throw new ArgumentException("Incorect value in id field");


            return sb.ToString();

        }

        public string Delete(T entity)
        {
            Type type = typeof(T);
            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = UniversalMethod.SanitizeName(AttributeNameTable?.Name ?? type.Name);

            List<TableField> table;
            if (!_cache.TryGet(NameTable, out table))
                throw new ArgumentException("Don`t cached table");

            string IdField = table.Single(x => x.IsPrimaryKey).FieldName;
            var IdPropertyVal = type.GetProperty(IdField).GetValue(entity);

            if (IdPropertyVal is int)
                return $"DELETE FROM {NameTable} WHERE {IdField} = {IdPropertyVal}";
            else if (IdPropertyVal is Guid)
                return $"DELETE FROM {NameTable} WHERE {IdField} = '{IdPropertyVal}'";
            else throw new ArgumentException("Incorect value in id field");

        }
    }



}
