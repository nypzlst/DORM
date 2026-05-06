using DORM.Attribute;
using DORM.Exceptions;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Infrastructure.Core
{
    public class Database
    {
        string NameDatabase { get; set; }
        string Server { get; set; }
        string User { get; set; }
        string Password { get; set; }
        int Port { get; set; }

        string QueryConn;

        List<ParametrizationQuery> _pendingQueries;


        public Database(string queryConn)
        {
            if (!File.Exists(queryConn))
                throw new ArgumentException("Incorrect path");
            QueryConn = File.ReadAllText(queryConn).Trim();
            var builder = new MySqlConnectionStringBuilder(QueryConn);
            NameDatabase = builder.Database;
            Server = builder.Server;
            User = builder.UserID;
            Password = builder.Password;
            Port = (int)builder.Port;
            _pendingQueries = new();
        }

        public Database(string namedb, string server, string user, string password, int port = 3306)
        {
            NameDatabase = namedb;
            Server = server;
            User = user;
            Password = password;
            Port = port;
            _pendingQueries = new();
        }

        internal string constructConnectionString()
        {
            return $"Server={Server};Database={NameDatabase};User ID={User};Password={Password};Port={Port};";
        }

        /// <summary>
        /// Возвращает строку подключения, лениво кэшируя её в QueryConn после первого построения.
        /// </summary>
        private string GetConnectionString()
        {
            if (string.IsNullOrEmpty(QueryConn))
                QueryConn = constructConnectionString();
            return QueryConn;
        }


        public async Task CheckConnection()
        {
            try
            {
                await using var connection = new MySqlConnection(GetConnectionString());
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new DormExecutionException($"Unable to connect to the database: {ex.Message}");
            }
        }

        //TODO: ExecuteAsync(string sql) — для INSERT, UPDATE, DELETE, CREATE TABLE. Відкриває з'єднання → створює MySqlCommand → виконує → закриває.
        //TODO: QueryAsync<T>(string sql) — для SELECT.Відкриває з'єднання → читає результат через MySqlDataReader → маппить рядки назад у List<T> → закриває.


        public List<T> SelectQuery<T>(string query) where T : class
        {
            using var connection = new MySqlConnection(GetConnectionString());
            connection.Open();

            using var command = new MySqlCommand(query, connection);
            List<T> tableSelect = new();

            using(MySqlDataReader reader = command.ExecuteReader())
            {
                // TODO: add try/catch
                while (reader.Read())
                {
                    var temp = Activator.CreateInstance<T>();
                    Type type = typeof(T);

                    foreach(var property in type.GetProperties())
                    {
                        var colName = property.GetCustomAttribute<NameAttribute>()?.Name ?? property.Name;
                        if (reader[colName] != DBNull.Value)
                        {
                            property.SetValue(temp, reader[colName]);
                        }
                    }
                    tableSelect.Add(temp);
                }
            }
            return tableSelect;
        }

        public void SaveToDb()
        {
            if (_pendingQueries.Count == 0) return;

            using var connection = new MySqlConnection(GetConnectionString());
            try
            {
                connection.Open();

                var bt = connection.BeginTransaction();

                try
                {
                    using var batch = new MySqlBatch(connection, bt);
                    foreach (var query in _pendingQueries)
                    {
                        var batchCommand = new MySqlBatchCommand(query.Sql);
                        foreach (var param in query.Parameters)
                        {
                            batchCommand.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        batch.BatchCommands.Add(batchCommand);
                    }

                    batch.ExecuteNonQuery();
                    bt.Commit();
                    _pendingQueries.Clear();

                }
                catch
                {
                    bt.Rollback();
                    throw;
                }
            }
            catch(Exception ex)
            {
                throw new DormExecutionException("[DORM] Execution error: " + ex.Message);
            }
        }


        internal void AddToQuery(ParametrizationQuery query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            _pendingQueries.Add(query);
        }

        public void ExecuteRaw(string sql)
        {
            using var connection = new MySqlConnection(GetConnectionString());
            connection.Open();
            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }


        

    }
}
