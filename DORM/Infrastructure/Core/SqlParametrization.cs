using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Core
{
    public class SqlParametrization
    {
        internal string Sql { get; set; }
        internal Dictionary<string,object> Parameters { get; set; }

        public SqlParametrization(string Query, Dictionary<string, object> parameters)
        {
            Sql = Query;
            Parameters = parameters ?? new Dictionary<string,object>();
        }
    }
}
