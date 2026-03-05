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

        Database(string queryConn)
        {
            if (!File.Exists(queryConn))
                throw new ArgumentException("Incorrect path");
            // Check if file not null or other type
            QueryConn = queryConn;
        }

        Database(string namedb, string server, string user, string password, int port = 3306)
        {
            NameDatabase = namedb;
            Server = server;
            User = user;
            Password = password;
            Port = port;
        }

        string constructConnectionString()
        {
            return $"Server={Server};Database={NameDatabase};User ID={User};Password={Password};Port={Port};";
        }


        async Task<bool> CheckConnection()
        {
            if (string.IsNullOrEmpty(QueryConn))
                QueryConn = constructConnectionString();
            await using var connection = new MySqlConnection(QueryConn);

            return await connection.PingAsync();
        }
    }
}
