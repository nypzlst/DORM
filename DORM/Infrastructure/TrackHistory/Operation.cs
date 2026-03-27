using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.TrackHistory
{
    internal class Operation
    {
        public Guid Id { get; set; } 
        
        public EOperationType TypeOperation { get; set; }
        public EOperationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public object? OldValue {  get; set; }
        public object? NewValue { get; set; }
        public int ChangedFields { get; set; }

        public string? EntityName { get; set; }
        public string? EntityKey { get; set; }

        

        public Operation(EOperationType type, object oldV, object newV, string entityName, string entityKey)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Status = EOperationStatus.Pending;

            TypeOperation = type;
            OldValue = oldV;
            NewValue = newV;
            EntityName = entityName;
            EntityKey = entityKey;
        }

        public Operation(EOperationType type, object newV, string entityName, string entityKey)
        {
            Id = Guid.NewGuid();
            CreatedAt= DateTime.UtcNow;
            Status = EOperationStatus.Pending;

            TypeOperation = type;
            NewValue= newV;
            EntityName = entityName;
            EntityKey = entityKey;
        }
    }
}
