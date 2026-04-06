using DORM.Attribute;
using DORM.Exceptions;
using DORM.Infrastructure.Cache;
using DORM.Infrastructure.CRUD;
using DORM.Infrastructure.TrackHistory;
using DORM.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DORM.Infrastructure.Core
{
    public class DormContext
    {

        private readonly Database _db;
        private readonly ICacheController _cache;
        private readonly ICrudQuery _crudQuery;
        private readonly TrackChanges _trackChanges;

        public DormContext(Database db, ICrudQuery crudQuery, ICacheController cache)
        {
            _db = db;
            _cache = cache;
            _crudQuery = crudQuery;
            _trackChanges = new TrackChanges();
        }


        public List<TResult> Select<T, TResult>(Expression<Func<T, TResult>> expression) where T : class where TResult : class
        {
            string sql = _crudQuery.Select<T, TResult>(expression);
            return _db.SelectQuery<TResult>(sql);
        }


        public void Update<T> (T entity) where T : class
        {
            var builder = _crudQuery.Update<T>(entity);

            var tableName = ResolveTableName<T>();
            var tableKey = ResolvePrimaryKeyField<T>();
           

            _trackChanges.TrackUpdate(entity, tableName, tableKey);

            _db.AddToQuery(builder);
        }

        public void Delete<T> (T entity) where T : class
        {
            var builder = _crudQuery.Delete<T>(entity);

            var tableName = ResolveTableName<T>();
            var tableKey = ResolvePrimaryKeyField<T>();
            _trackChanges.TrackDelete(entity, tableName,tableKey);

            _db.AddToQuery(builder);
        }

        public void Insert<T> (T entity) where T : class
        {
            var builder = _crudQuery.Insert<T>(entity);

            var tableName = ResolveTableName<T>();
            var tableKey = ResolvePrimaryKeyField<T>();
            _trackChanges.TrackInsert(entity, tableName, tableKey);

            _db.AddToQuery(builder);
        }

        public async Task SaveChanges()
        {
            try
            {
                await _db.CheckConnection();
                _db.SaveToDb();

                _trackChanges.MarkCommitted();
            }
            catch (ConnectionException ex)
            {
                Console.WriteLine($"[DORM] Connection error: {ex.Message}");
                _trackChanges.MarkFailed();
                throw;
            }
            catch
            {
                _trackChanges.MarkFailed();
                throw;
            }
        }




        #region system methods

        private static string ResolveTableName<T>() where T : class
        {
            var type = typeof(T);
            var nameAttr = (NameAttribute?)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            return UniversalMethod.SanitizeName(nameAttr?.Name ?? type.Name);
        }
        private static string ResolvePrimaryKeyField<T>() where T : class
        {
            var table = MappingClass.MapClass<T>();
            var pk = table.SingleOrDefault(f => f.IsPrimaryKey);
            return pk?.FieldName ?? "Id"; // тут змінити якщо хочу читати дані поля а не знати ключ
        }

        #endregion
    }
}
