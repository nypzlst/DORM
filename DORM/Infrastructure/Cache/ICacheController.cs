using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Cache
{
    public interface ICacheController
    {
        void Store(string tableName, List<TableField> tableFields);
        bool TryGet(string tableName, out List<TableField> tableFields);



    }
}
