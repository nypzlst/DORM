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

        public Database(string queryConn)
        {
            if (!File.Exists(queryConn))
                throw new ArgumentException("Incorrect path");
            // Check if file not null or other type
            QueryConn = queryConn;
        }

        public Database(string namedb, string server, string user, string password, int port = 3306)
        {
            NameDatabase = namedb;
            Server = server;
            User = user;
            Password = password;
            Port = port;
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
    }
}
