using DORM.CLI.Interfaces;
using DORM.CLI.Providers;
using Spectre.Console;
using TestORM; // For DotEnv

namespace DORM.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Load Environment Variables
        DotEnv.Load();
        
        string dbType = Environment.GetEnvironmentVariable("DB_TYPE") ?? "mysql";
        string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "";
        
        // Fallback: build connection string from separate variables (Variant 2 in .env)
        if (string.IsNullOrEmpty(connectionString))
        {
            string host = Environment.GetEnvironmentVariable("DORM_HOST") ?? "localhost";
            string user = Environment.GetEnvironmentVariable("DORM_USER") ?? "root";
            string pass = Environment.GetEnvironmentVariable("DORM_PASS") ?? "";
            string port = Environment.GetEnvironmentVariable("DORM_PORT") ?? (dbType == "postgres" ? "5432" : "3306");
            string db = Environment.GetEnvironmentVariable("DORM_DB") ?? "information_schema";

            if (dbType.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
            }
            else
            {
                connectionString = $"Server={host};Port={port};Database={db};Uid={user};Pwd={pass};";
            }
        }

        string dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? Environment.GetEnvironmentVariable("DORM_DB") ?? "testdb";

        // Fallback check: Did it load anything at all?
        if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("Uid=;")) // Simple check if it was built completely empty
        {
            AnsiConsole.MarkupLine("[red]Error:[/] CONNECTION_STRING or DORM_* variables are missing.");
            AnsiConsole.MarkupLine("[yellow]Hint:[/] Ensure your .env file contains either CONNECTION_STRING=... or DORM_HOST, DORM_USER, DORM_PASS, DORM_DB.");
            return;
        }

        // 2. Initialize Provider
        IDatabaseMonitor monitor;
        IDatabaseAdmin admin;

        if (dbType.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var pgProvider = new PostgresProvider(connectionString);
            monitor = pgProvider;
            admin = pgProvider;
        }
        else
        {
            var mySqlProvider = new MySqlProvider(connectionString);
            monitor = mySqlProvider;
            admin = mySqlProvider;
        }

        // 3. CLI Loop
        AnsiConsole.Write(
            new FigletText("DORM.CLI")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine($"[grey]Connected to:[/] {dbType.ToUpper()} | [grey]Database:[/] {dbName}");
        AnsiConsole.WriteLine();

        while (true)
        {
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an action:")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "Monitor Status", 
                        "List Tables", 
                        "Drop Table", 
                        "Truncate Table", 
                        "Exit"
                    }));

            try
            {
                switch (action)
                {
                    case "Monitor Status":
                        await ShowStatusAsync(monitor, dbName);
                        break;
                    case "List Tables":
                        await ShowTablesAsync(monitor, dbName);
                        break;
                    case "Drop Table":
                        await DropTablePromptAsync(admin, monitor, dbName);
                        break;
                    case "Truncate Table":
                        await TruncateTablePromptAsync(admin, monitor, dbName);
                        break;
                    case "Exit":
                        return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press Enter to continue...[/]");
            Console.ReadLine();
            AnsiConsole.Clear();
        }
    }

    private static async Task ShowStatusAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching server status...", async ctx =>
            {
                var version = await monitor.GetServerVersionAsync();
                var connections = await monitor.GetActiveConnectionsAsync();
                var dbSize = await monitor.GetDatabaseSizeAsync(dbName);

                var table = new Table();
                table.AddColumn("Metric");
                table.AddColumn("Value");

                table.AddRow("Version", $"[green]{version}[/]");
                table.AddRow("Active Connections", $"[yellow]{connections}[/]");
                table.AddRow("DB Size (Bytes)", $"[blue]{dbSize:N0}[/]");

                AnsiConsole.Write(table);
            });
    }

    private static async Task ShowTablesAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching tables...", async ctx =>
            {
                var tables = await monitor.GetTableSizesAsync(dbName);

                var table = new Table();
                table.AddColumn("Table Name");
                table.AddColumn("Size (Bytes)");

                foreach (var t in tables)
                {
                    table.AddRow(t.TableName, $"[blue]{t.SizeBytes:N0}[/]");
                }

                AnsiConsole.Write(table);
            });
    }

    private static async Task DropTablePromptAsync(IDatabaseAdmin admin, IDatabaseMonitor monitor, string dbName)
    {
        var tables = await monitor.GetTablesAsync(dbName);
        if (!tables.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tables found.[/]");
            return;
        }

        var tableToDrop = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a table to [red]DROP[/]:")
                .AddChoices(tables.Append("<Cancel>")));

        if (tableToDrop == "<Cancel>") return;

        if (AnsiConsole.Confirm($"Are you sure you want to drop [red]{Markup.Escape(tableToDrop)}[/]?", defaultValue: false))
        {
            await admin.DropTableAsync(tableToDrop);
            AnsiConsole.MarkupLine($"[green]Table {Markup.Escape(tableToDrop)} dropped successfully.[/]");
        }
    }

    private static async Task TruncateTablePromptAsync(IDatabaseAdmin admin, IDatabaseMonitor monitor, string dbName)
    {
         var tables = await monitor.GetTablesAsync(dbName);
        if (!tables.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tables found.[/]");
            return;
        }

        var tableToTruncate = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a table to [yellow]TRUNCATE[/]:")
                .AddChoices(tables.Append("<Cancel>")));

        if (tableToTruncate == "<Cancel>") return;

        if (AnsiConsole.Confirm($"Are you sure you want to truncate [yellow]{Markup.Escape(tableToTruncate)}[/]?", defaultValue: false))
        {
            await admin.TruncateTableAsync(tableToTruncate);
            AnsiConsole.MarkupLine($"[green]Table {Markup.Escape(tableToTruncate)} truncated successfully.[/]");
        }
    }
}