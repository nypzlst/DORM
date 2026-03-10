using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Cache
{
    internal class MemoryCacheController : ICacheController
    {
        private Dictionary<string, List<TableField>> CacheTable { get; set; } = new();

        
        public void Store(string tableName, List<TableField> tableFields)
        {
            if (CacheTable.ContainsKey(tableName))
                CacheTable[tableName] = tableFields;   
            else
                CacheTable.Add(tableName, tableFields);
        }

        public bool TryGet(string tableName, out List<TableField> tableFields)
        {
            return CacheTable.TryGetValue(tableName, out tableFields);
        }
    }
}
