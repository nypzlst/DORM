using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Providers
{
    public interface IDialect
    {
        IReadOnlyDictionary<string,string> TypeMap { get; }

        string QuoteIdentifier(string identifier);
    }
}
