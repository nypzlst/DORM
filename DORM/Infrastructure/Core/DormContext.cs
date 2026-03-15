using DORM.Exceptions;
using DORM.Infrastructure.Cache;
using DORM.Infrastructure.CRUD;
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

        public DormContext(Database db, ICrudQuery crudQuery)
        {
            _db = db;
            _cache = new MemoryCacheController();
            _crudQuery = crudQuery;
        }


        public List<TResult> Select<T, TResult>(Expression<Func<T, TResult>> expression) where T : class where TResult : class
        {
            string sql = _crudQuery.Select<T, TResult>(expression);
            return _db.SelectQuery<TResult>(sql);
        }


        public void Update<T> (T entity) where T : class
        {
            var builder = _crudQuery.Update<T>(entity);
            _db.AddToQuery(builder);
        }

        public void Delete<T> (T entity) where T : class
        {
            var builder = _crudQuery.Delete<T>(entity);
            _db.AddToQuery(builder);
        }

        public void Insert<T> (T entity) where T : class
        {
            var builder = _crudQuery.Insert<T>(entity);
            _db.AddToQuery(builder);
        }

        public async Task SaveChanges()
        {
            try
            {
                await _db.CheckConnection();
                _db.SaveToDb();
            }
            catch (ConnectionException ex)
            {
                Console.WriteLine($"[DORM] Connection error: {ex.Message}");
                throw;
            }
        }
    }
}
