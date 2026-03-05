using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Infrastructure
{
    public class Database
    {
        string NameDatabase { get; set; }
        string Server { get; set; }
        string User { get; set; }
        string Password { get; set; }
        int Port { get; set; }


        Database(string queryConn)
        {
            if (!File.Exists(queryConn))
                throw new ArgumentException("Incorrect path");
        }

        Database(string namedb, string server ,string user, string password, int port = 3306)
        {
            NameDatabase = namedb;
            Server = server;
            User = user;
            Password = password;
            Port = port;
        }




    }
}
