using System;
using System.Collections.Generic;
using System.Text;
using MySqlConnector;

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
            // Check if file not null or other type
            QueryConn = queryConn;
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


        internal async Task<bool> CheckConnection()
        {
            if (string.IsNullOrEmpty(QueryConn))
                QueryConn = constructConnectionString();
            await using var connection = new MySqlConnection(QueryConn);

            return await connection.PingAsync();
        }
        //TODO: Додати зберігання готового connection string як поля після першої побудови
        //TODO: ExecuteAsync(string sql) — для INSERT, UPDATE, DELETE, CREATE TABLE. Відкриває з'єднання → створює MySqlCommand → виконує → закриває.
        //TODO: QueryAsync<T>(string sql) — для SELECT.Відкриває з'єднання → читає результат через MySqlDataReader → маппить рядки назад у List<T> → закриває.

        
       List<T> SelectQuery<T>(string query) where T : class
        {
            using var connection = new MySqlConnection(constructConnectionString());
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
                        if(reader[property.Name] != DBNull.Value)
                        {
                            property.SetValue(temp, reader[property.Name]);
                        }
                    }
                    tableSelect.Add(temp);
                }
            }
            return tableSelect;
        }

        void SaveToDb()
        {
            using var connection = new MySqlConnection(constructConnectionString());
            connection.Open();

            using var batch = new MySqlBatch(connection);

            foreach(var query in _pendingQueries)
            {
                var batchCommand = new MySqlBatchCommand(query.Sql);
                foreach(var param in query.Parameters)
                {
                    batchCommand.Parameters.AddWithValue(param.Key,param.Value);
                }

                batch.BatchCommands.Add(batchCommand);
            }

            batch.ExecuteNonQuery();
            _pendingQueries.Clear();
        }


        void AddToQuery(ParametrizationQuery query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            _pendingQueries.Add(query);
        }


    }
}
