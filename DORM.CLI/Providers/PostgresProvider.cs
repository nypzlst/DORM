using System.Data.Common;
using System.Text;
using DORM.CLI.Interfaces;
using DORM.CLI.Models;
using Npgsql;

namespace DORM.CLI.Providers;

public class PostgresProvider : IDatabaseMonitor, IDatabaseAdmin
{
    private readonly string _connectionString;

    public PostgresProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    // --- IDatabaseMonitor ---

    public async Task<string> GetServerVersionAsync()
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT version()", connection);
        return (await command.ExecuteScalarAsync())?.ToString() ?? "Unknown";
    }

    public async Task<int> GetActiveConnectionsAsync()
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'", connection);
        var result = await command.ExecuteScalarAsync();
        return result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    public async Task<IEnumerable<string>> GetTablesAsync(string dbName)
    {
        var tables = new List<string>();
        await using var connection = await GetConnectionAsync();
        // Ignoring dbName for Postgres in this basic implementation as connections are usually per-DB
        var query = "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema'";
        await using var command = new NpgsqlCommand(query, connection);
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
        var query = "SELECT relname as TABLE_NAME, pg_total_relation_size(relid) as SizeBytes FROM pg_catalog.pg_statio_user_tables";
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stats.Add(new TableStatistic(
                reader.GetString(reader.GetOrdinal("TABLE_NAME")),
                Convert.ToInt64(reader["SizeBytes"])
            ));
        }
        return stats;
    }

    public async Task<string> GetServerStatusAsync()
    {
        var status = new StringBuilder();
        await using var connection = await GetConnectionAsync();
        var query = "SELECT pg_postmaster_start_time() as Uptime, pg_database_size(current_database()) as DbSize";
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            status.AppendLine($"Started: {reader["Uptime"]}");
            status.AppendLine($"Current DB Size: {reader["DbSize"]} bytes");
        }
        return status.ToString().TrimEnd();
    }

    public async Task<long> GetDatabaseSizeAsync(string dbName)
    {
        await using var connection = await GetConnectionAsync();
        // Assuming connection is already to the target DB
        await using var command = new NpgsqlCommand("SELECT pg_database_size(current_database())", connection);
        var result = await command.ExecuteScalarAsync();
        return result != DBNull.Value ? Convert.ToInt64(result) : 0;
    }

    // --- IDatabaseAdmin ---

    public async Task DropTableAsync(string tableName)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand($"DROP TABLE IF EXISTS \"{tableName}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task TruncateTableAsync(string tableName)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand($"TRUNCATE TABLE \"{tableName}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> TableExistsAsync(string tableName, string dbName)
    {
        await using var connection = await GetConnectionAsync();
        var query = "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename  = @tableName)";
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    public async Task<IEnumerable<string>> GetIndexesAsync(string tableName)
    {
        var indexes = new List<string>();
        await using var connection = await GetConnectionAsync();
        var query = "SELECT indexname FROM pg_indexes WHERE tablename = @tableName";
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }
        return indexes;
    }
}