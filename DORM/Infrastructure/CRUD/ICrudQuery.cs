using DORM.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DORM.Infrastructure.CRUD
{
    internal interface ICrudQuery <T> where T : class 
    {
        string CreateTable(T entity);
        string Select<TResult>(Expression<Func<T, TResult>> expression);
        ParametrizationQuery Update(T entity);
        ParametrizationQuery Delete(T entity);
        ParametrizationQuery Insert(T entity);
    }
}
