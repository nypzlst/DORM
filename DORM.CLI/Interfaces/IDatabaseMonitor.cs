using DORM.CLI.Models;

namespace DORM.CLI.Interfaces;

public interface IDatabaseMonitor
{
    Task<string> GetServerVersionAsync();
    Task<int> GetActiveConnectionsAsync();
    Task<IEnumerable<string>> GetTablesAsync(string dbName);
    Task<IEnumerable<TableStatistic>> GetTableSizesAsync(string dbName);
    Task<string> GetServerStatusAsync();
    Task<long> GetDatabaseSizeAsync(string dbName);
}