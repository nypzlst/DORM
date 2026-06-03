namespace DORM.CLI.Interfaces;

public interface IDatabaseAdmin
{
    Task DropTableAsync(string tableName);
    Task TruncateTableAsync(string tableName);
    Task<bool> TableExistsAsync(string tableName, string dbName);
    Task<IEnumerable<string>> GetIndexesAsync(string tableName);
}