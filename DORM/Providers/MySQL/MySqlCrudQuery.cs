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

    public class MySqlCrudQuery : ICrudQuery
    {
        private readonly ICacheController _cache;

        public MySqlCrudQuery(ICacheController cache)
        {
            _cache = cache;
        }

        public string CreateTable<T>(T entity) where T : class
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

        // param no, make when WHERE is implement
        public string Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class
        {
            Type type = typeof(T);
            string NameTable = AdditionalQueryMethod.GetNameTable<T>();
            List<TableField> table = AdditionalQueryMethod.GetCachedTable<T>(NameTable, _cache);
            

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


        public ParametrizationQuery Update<T>(T entity) where T : class
        {
            string TableName = AdditionalQueryMethod.GetNameTable<T>();

            List<TableField> Table = AdditionalQueryMethod.GetCachedTable<T>(TableName,_cache);

            var pkInfo = AdditionalQueryMethod.GetPrimaryKey(Table,entity);

            string IdField = pkInfo.idFieldName;
            var IdPropertyVal = pkInfo.idFieldValue;
             
            var sb = new StringBuilder();
            var changesList = new List<string>();

            Dictionary<string, object> param = new();

            sb.Append("UPDATE ").Append(TableName).Append(" SET ");
            foreach(PropertyInfo info in typeof(T).GetProperties())
            {
                
                var field = Table.SingleOrDefault(x => x.FieldName == info.Name);
                var fieldValue = info.GetValue(entity);

                if (field is not null && !field.IsPrimaryKey)
                {

                    changesList.Add($"{field.FieldName} = @{field.FieldName}");

                    param.Add($"@{field.FieldName}", fieldValue ?? DBNull.Value);
                }
            }

            sb.Append(string.Join(", ", changesList));
            param.Add($"@{IdField}", IdPropertyVal);

            AdditionalQueryMethod.BuildWhereById(sb, IdField, IdPropertyVal);

            return new ParametrizationQuery(sb.ToString(), param, TypeQuery.update);

        }

        public ParametrizationQuery Delete<T>(T entity) where T : class
        {
            string tableName = AdditionalQueryMethod.GetNameTable<T>();

            var table = AdditionalQueryMethod.GetCachedTable<T>(tableName, _cache);
            var pkInfo = AdditionalQueryMethod.GetPrimaryKey(table, entity);

            var sb = new StringBuilder();

            sb.Append("DELETE FROM ").Append(tableName);
            AdditionalQueryMethod.BuildWhereById(sb, pkInfo.idFieldName, pkInfo.idFieldValue);
            Dictionary<string, object> param = new() { { $"@{pkInfo.idFieldName}", pkInfo.idFieldValue } };


            return new ParametrizationQuery(sb.ToString(), param, TypeQuery.delete);

        }

        public ParametrizationQuery Insert<T>(T entity) where T : class
        {
            StringBuilder sb = new StringBuilder();
            string tableName = AdditionalQueryMethod.GetNameTable<T>();

            sb.Append("INSERT INTO ").Append(tableName).Append(" (");
            var table = AdditionalQueryMethod.GetCachedTable<T>(tableName,_cache);
            List<string> fieldToInsert = new();
            List<object> fieldPlaceholder = new();
            Dictionary<string,object> param = new();

            foreach (PropertyInfo info in typeof(T).GetProperties())
            {
                var checkField = table.SingleOrDefault(x => x.FieldName == info.Name);
                if (checkField != null && !checkField.IsPrimaryKey)
                {
                    fieldToInsert.Add(info.Name);
                    fieldPlaceholder.Add($"@{info.Name}");

                    var val = info.GetValue(entity);

                    param.Add($"@{info.Name}", val ?? DBNull.Value);
                }
            }
            sb.Append(string.Join(", ", fieldToInsert)).Append(") ").Append("VALUES ");
            sb.Append("( ").Append(string.Join(", ",fieldPlaceholder)).Append("); ");
            return new ParametrizationQuery(sb.ToString(), param, TypeQuery.insert);
        }
    }



}
