using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Core
{
    public class ParametrizationQuery
    {
        internal string Sql { get; set; }
        internal Dictionary<string,object> Parameters { get; set; }

        public ParametrizationQuery(string Query, Dictionary<string, object> parameters)
        {
            Sql = Query;
            Parameters = parameters ?? new Dictionary<string,object>();
        }
    }
}
