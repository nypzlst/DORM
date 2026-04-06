using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure.TrackHistory
{
    public class TrackChanges
    {
        private List<Operation> _operations = [];

        public IReadOnlyList<Operation> Operations => _operations.AsReadOnly();

        public void TrackInsert(object insertValue, string tableName, string tableField)
        {
            if(insertValue is null) throw new ArgumentNullException(nameof(insertValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableField)) throw new ArgumentException("Entity key is required.");


            var InsertOperation = new Operation(EOperationType.Insert,
                insertValue, tableName, tableField);

            _operations.Add(InsertOperation);
        }


        public void TrackDelete(object deleteValue, string tableName, string tableField)
        {
            if (deleteValue is null) throw new ArgumentNullException(nameof(deleteValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableField)) throw new ArgumentException("Entity key is required.");


            var DeleteOperation = new Operation(EOperationType.Delete,
                deleteValue, tableName, tableField);

            _operations.Add(DeleteOperation);
        }


        public void TrackUpdate(object updateValue, object oldValue ,string tableName, string tableField)
        {
            if (updateValue is null) throw new ArgumentNullException(nameof(updateValue));
            if (oldValue is null) throw new ArgumentNullException(nameof(oldValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableField)) throw new ArgumentException("Entity key is required.");

            var UpdateOperation = new Operation(EOperationType.Update, oldValue, updateValue, tableName, tableField);

            _operations.Add(UpdateOperation);
        }


        public void SetStatus(Guid id, EOperationStatus status)
        {

            var findOper = _operations.FirstOrDefault(x => id == x.Id);
            if(findOper is not null)
            {
                findOper.Status = status;
            }
            else
            {
                throw new InvalidOperationException("Operation not found");
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
