using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.TrackHistory
{
    public class TrackChanges
    {
        private List<Operation> _operations;

        public void TrackInsert(object insertValue, string tableName, string tableField)
        {
            if(insertValue is null) throw new ArgumentException(nameof(insertValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableField)) throw new ArgumentException("Entity key is required.");


            var InsertOperation = new Operation(EOperationType.Insert,
                insertValue, tableName, tableField);

            _operations.Add(InsertOperation);
        }



        public void SetStatus(Operation operation, EOperationStatus status)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            var findOper = _operations.FirstOrDefault(x => operation.Id == x.Id);
            if(findOper is not null)
            {
                findOper.Status = status;
            }
        }
        
        public void MarkAll(EOperationStatus status)
        {
            foreach(var x in _operations)
                x.Status = status;
        }

        public void MarkCommited() => MarkAll(EOperationStatus.Committed);

        public void MarkFailed() => MarkAll(EOperationStatus.Failed);
    }
}
