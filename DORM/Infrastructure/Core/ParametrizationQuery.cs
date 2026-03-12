using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Core
{
    public class ParametrizationQuery
    {
        internal string Sql { get; set; }
        internal Dictionary<string,object> Parameters { get; set; }

        internal TypeQuery Type { get; set; }

        public ParametrizationQuery(string Query, Dictionary<string, object> parameters, TypeQuery type)
        {
            Sql = Query;
            Parameters = parameters ?? new Dictionary<string,object>();
            Type = type;
        }
    }

    public enum TypeQuery
    {
        insert,
        update, 
        delete,
        createTable
    }
}
