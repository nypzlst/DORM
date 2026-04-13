
namespace DORM.Infrastructure.TrackHistory
{
    public class TrackChanges
    {
        private List<Operation> _operations = [];

        public event Action<Operation>? OnOperationTracked;

        public IReadOnlyList<Operation> Operations => _operations.AsReadOnly();

        public void TrackInsert(object insertValue, string tableName, string tableKey)
        {
            if(insertValue is null) throw new ArgumentNullException(nameof(insertValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableKey)) throw new ArgumentException("Entity key is required.");


            var InsertOperation = new Operation(EOperationType.Insert,
                insertValue, tableName, tableKey);

            _operations.Add(InsertOperation);
            OnOperationTracked?.Invoke(InsertOperation);
        }


        public void TrackDelete(object deleteValue, string tableName, string tableKey)
        {
            if (deleteValue is null) throw new ArgumentNullException(nameof(deleteValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableKey)) throw new ArgumentException("Entity key is required.");


            var DeleteOperation = new Operation(EOperationType.Delete,
                deleteValue, tableName, tableKey);

            _operations.Add(DeleteOperation);
            OnOperationTracked?.Invoke(DeleteOperation);
        }


        public void TrackUpdate(object updateValue,string tableName, string tableField)
        {
            if (updateValue is null) throw new ArgumentNullException(nameof(updateValue));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Entity name is required.");
            if (string.IsNullOrWhiteSpace(tableField)) throw new ArgumentException("Entity key is required.");

            var UpdateOperation = new Operation(EOperationType.Update, updateValue, tableName, tableField);

            _operations.Add(UpdateOperation);
            OnOperationTracked?.Invoke(UpdateOperation);
        }


        public void SetStatus(Guid id, EOperationStatus status)
        {

            var findOper = _operations.FirstOrDefault(x => id == x.Id);
            if(findOper is not null)
            {
                findOper.Status = status;
                OnOperationTracked?.Invoke(findOper);
            }
            else
            {
                throw new InvalidOperationException("Operation not found");
            }
        }
        
        public void MarkAll(EOperationStatus status)
        {
            foreach(var x in _operations.Where(x=> x.Status== EOperationStatus.Pending))
            {
                x.Status = status;
                OnOperationTracked?.Invoke(x);
            }
        }

        public void MarkCommitted() => MarkAll(EOperationStatus.Committed);

        public void MarkFailed() => MarkAll(EOperationStatus.Failed);
    }
}
