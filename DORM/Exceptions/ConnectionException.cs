using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Exceptions
{
    class ConnectionException : Exception
    {
        public ConnectionException() { }

        public ConnectionException(string message) : base(message)
        {
        }
    }
}
