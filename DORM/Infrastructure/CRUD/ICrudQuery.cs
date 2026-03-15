using DORM.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DORM.Infrastructure.CRUD
{
    public interface ICrudQuery
    {
        string CreateTable<T>(T entity) where T : class;
        string Select<T, TResult>(Expression<Func<T, TResult>> expression) where T : class;
        ParametrizationQuery Update<T>(T entity) where T : class;
        ParametrizationQuery Delete<T>(T entity) where T : class;
        ParametrizationQuery Insert<T>(T entity) where T : class;
    }
}
