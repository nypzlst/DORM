using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Exceptions
{
    public class DormExecutionException : Exception
    {
        public DormExecutionException() { }

        public DormExecutionException(string message) : base(message)
        {
        }
    }
}
