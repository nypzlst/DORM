using DORM.Infrastructure.TrackHistory;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Logger
{
    internal class BlankLogger : ILogger
    {
        public void Log(Operation op) { }
    }
}
