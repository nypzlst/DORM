using System.Data.Common;
using System.Text;
using DORM.CLI.Interfaces;
using DORM.CLI.Models;
using MySqlConnector;

namespace DORM.CLI.Providers;

public class MySqlProvider : IDatabaseMonitor, IDatabaseAdmin
{
    private readonly string _connectionString;

    public MySqlProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    // --- IDatabaseMonitor ---

    public async Task<string> GetServerVersionAsync()
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT VERSION()", connection);
        return (await command.ExecuteScalarAsync())?.ToString() ?? "Unknown";
    }

    public async Task<int> GetActiveConnectionsAsync()
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SHOW STATUS LIKE 'Threads_connected'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Convert.ToInt32(reader["Value"]);
        }
        return 0;
    }

    public async Task<IEnumerable<string>> GetTablesAsync(string dbName)
    {
        var tables = new List<string>();
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbName", connection);
        command.Parameters.AddWithValue("@dbName", dbName);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    public async Task<IEnumerable<TableStatistic>> GetTableSizesAsync(string dbName)
    {
        var stats = new List<TableStatistic>();
        await using var connection = await GetConnectionAsync();
        var query = @"
            SELECT TABLE_NAME, (DATA_LENGTH + INDEX_LENGTH) as SizeBytes 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = @dbName";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@dbName", dbName);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stats.Add(new TableStatistic(
                reader.GetString("TABLE_NAME"),
                Convert.ToInt64(reader["SizeBytes"])
            ));
        }
        return stats;
    }

    public async Task<string> GetServerStatusAsync()
    {
        var status = new StringBuilder();
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SHOW STATUS WHERE Variable_name IN ('Uptime', 'Questions', 'Slow_queries')", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            status.AppendLine($"{reader["Variable_name"]}: {reader["Value"]}");
        }
        return status.ToString().TrimEnd();
    }

    public async Task<long> GetDatabaseSizeAsync(string dbName)
    {
        await using var connection = await GetConnectionAsync();
        var query = @"
            SELECT SUM(DATA_LENGTH + INDEX_LENGTH) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = @dbName";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@dbName", dbName);
        var result = await command.ExecuteScalarAsync();
        return result != DBNull.Value ? Convert.ToInt64(result) : 0;
    }

    // --- IDatabaseAdmin ---

    public async Task DropTableAsync(string tableName)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand($"SET FOREIGN_KEY_CHECKS=0; DROP TABLE IF EXISTS `{tableName}`; SET FOREIGN_KEY_CHECKS=1;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task TruncateTableAsync(string tableName)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand($"SET FOREIGN_KEY_CHECKS=0; TRUNCATE TABLE `{tableName}`; SET FOREIGN_KEY_CHECKS=1;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> TableExistsAsync(string tableName, string dbName)
    {
        await using var connection = await GetConnectionAsync();
        var query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbName AND TABLE_NAME = @tableName";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@dbName", dbName);
        command.Parameters.AddWithValue("@tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public async Task<IEnumerable<string>> GetIndexesAsync(string tableName)
    {
        var indexes = new List<string>();
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand($"SHOW INDEX FROM `{tableName}`", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString("Key_name"));
        }
        return indexes.Distinct();
    }
}