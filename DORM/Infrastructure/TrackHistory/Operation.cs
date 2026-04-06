using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.TrackHistory
{
    public class Operation
    {
        public Guid Id { get; init; } 
        
        public EOperationType TypeOperation { get; init; }
        public EOperationStatus Status { get; set; }
        public DateTime CreatedAt { get; init; }

        public object? Value { get; set; }
        //public int ChangedFields { get; set; } create soon

        public string? EntityName { get; set; }
        public string? EntityKey { get; set; }

        
        public Operation(EOperationType type, object value, string entityName, string entityKey)
        {
            Id = Guid.NewGuid();
            CreatedAt= DateTime.UtcNow;
            Status = EOperationStatus.Pending;

            TypeOperation = type;
            Value= value;
            EntityName = entityName;
            EntityKey = entityKey;
        }
    }
}
