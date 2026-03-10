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

        public string CreateTable(T entity) 
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
        // select без умови, лише по колонкам 
        public string Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            Type type = typeof(T);
            string NameTable = AdditionalQueryMethod.GetNameTable<T>();
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
            string TableName = AdditionalQueryMethod.GetNameTable<T>();

            List<TableField> Table = AdditionalQueryMethod.GetCachedTable(TableName,_cache);

            var pkInfo = AdditionalQueryMethod.GetPrimaryKey(Table,entity);

            string IdField = pkInfo.idFieldName;
            var IdPropertyVal = pkInfo.idFieldValue;
             
            var sb = new StringBuilder();
            var changesList = new List<string>();

            sb.Append("UPDATE ").Append(TableName).Append(" SET ");
            foreach(PropertyInfo info in typeof(T).GetProperties())
            {
                
                var field = Table.SingleOrDefault(x => x.FieldName == info.Name);
                var fieldValue = info.GetValue(entity);

                if (field is not null)
                {
                    if (field.IsPrimaryKey) continue;

                    if (fieldValue is string || fieldValue is DateTime)
                        changesList.Add($"{field.FieldName} = '{fieldValue}'");
                    else if (fieldValue is bool b)
                        changesList.Add($"{field.FieldName} = {(b ? 1 : 0)}");
                    else if (fieldValue is null)
                        changesList.Add($"{field.FieldName} = NULL");
                    else
                        changesList.Add($"{field.FieldName} = {fieldValue}");
                }
            }

            sb.Append(string.Join(", ", changesList));

            AdditionalQueryMethod.BuildWhereById(sb, IdField, IdPropertyVal);

            return sb.ToString();

        }

        public string Delete(T entity)
        {
            string tableName = AdditionalQueryMethod.GetNameTable<T>();

            var table = AdditionalQueryMethod.GetCachedTable(tableName, _cache);
            var pkInfo = AdditionalQueryMethod.GetPrimaryKey(table, entity);

            var sb = new StringBuilder();

            sb.Append("DELETE FROM ").Append(tableName);
            AdditionalQueryMethod.BuildWhereById(sb, pkInfo.idFieldName, pkInfo.idFieldValue);
            return sb.ToString();

        }

        public string Insert(T entity)
        {
            StringBuilder sb = new StringBuilder();
            string tableName = AdditionalQueryMethod.GetNameTable<T>();

            sb.Append("INSERT INTO ").Append(tableName).Append(" (");
            var table = AdditionalQueryMethod.GetCachedTable(tableName,_cache);
            List<string> fieldToInsert = new();
            List<object> fieldValues = new();

            foreach (PropertyInfo info in typeof(T).GetProperties())
            {
                var checkField = table.SingleOrDefault(x => x.FieldName == info.Name);
                if (checkField != null && !checkField.IsPrimaryKey)
                {
                    fieldToInsert.Add(info.Name);

                    var val = info.GetValue(entity);
                    if (val is string)
                        fieldValues.Add($"'{val}'");
                    if (val is DateTime dt)
                        fieldValues.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                    else if (val is bool b)
                        fieldValues.Add(b ? 1 : 0);
                    else if (val is null)
                        fieldValues.Add("NULL");
                    else
                        fieldValues.Add(val);
                }
            }
            sb.Append(string.Join(", ", fieldToInsert)).Append(") ").Append("VALUES ");
            sb.Append("( ").Append(string.Join(", ",fieldValues)).Append("); ");
            return sb.ToString();
        }
    }



}
