using DORM.Infrastructure.TrackHistory;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.Logger
{
    internal interface ILogger
    {        
        void Log(Operation op);

    }
}
