using DORM.Infrastructure.TrackHistory;
using System.Text.Json;

namespace DORM.Infrastructure.Logger
{
    internal class JsonLogger : ILogger, IDisposable
    {
        private readonly Lock _lock = new();
        private readonly StreamWriter? _writer;
        private readonly string _logFileName;
        private bool _isFaulted;

        public JsonLogger()
        {
            _logFileName = Path.Combine(Directory.GetCurrentDirectory(), $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
            try
            {
                _writer = new StreamWriter(_logFileName, append: true) { AutoFlush = true };
            }
            catch
            {
                _isFaulted = true;
            }
        }


        public void Log(Operation op)
        {
           if (_isFaulted) return; 

           lock(_lock)
           {
                if (_isFaulted) return;
                if (_writer == null) return; 
                try
                {
                    string json = JsonSerializer.Serialize(new
                    {
                        level = op.Status switch
                        {
                            EOperationStatus.Failed => ELogLevel.Error,
                            EOperationStatus.Canceled => ELogLevel.Warn,
                            _ => ELogLevel.Info
                        },
                        op.Id,
                        op.Status,
                        op.TypeOperation,
                        op.CreatedAt,
                        op.EntityName,
                        op.EntityKey
                    });
                    _writer.WriteLine(json);
                }
                catch
                {
                    _isFaulted = true;
                }
           }
        }


        public void Dispose()
        {
            lock (_lock)
            {
                _isFaulted = true;
                _writer?.Dispose();
            }
        }

    }
}
