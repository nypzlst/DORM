using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.CRUD
{
    internal interface ICrudQuery <T> where T : class 
    {
        string Create(T entity);
        string Select(T entity);
        string Update(T entity);
        string Delete(T entity);
    }
}
