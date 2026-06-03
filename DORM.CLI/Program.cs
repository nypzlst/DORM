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
                        "View Statistics",
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
                    case "View Statistics":
                        await ShowStatisticsChartAsync(monitor, dbName);
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
                var extraMetrics = await monitor.GetServerStatusAsync();

                var table = new Table();
                table.AddColumn("Metric");
                table.AddColumn("Value");

                table.AddRow("Server Version", $"[green]{version}[/]");
                table.AddRow("DB Size (Bytes)", $"[blue]{dbSize:N0}[/]");
                table.AddRow("Active Connections", $"[yellow]{connections}[/]");
                
                if (extraMetrics.TryGetValue("Uptime", out var uptimeStr) && long.TryParse(uptimeStr, out var uptimeSeconds))
                {
                    var timeSpan = TimeSpan.FromSeconds(uptimeSeconds);
                    table.AddRow("Uptime", $"[cyan]{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m[/]");
                }
                
                if (extraMetrics.TryGetValue("Questions", out var questions))
                    table.AddRow("Total Queries", $"[magenta]{questions}[/]");
                    
                if (extraMetrics.TryGetValue("Slow_queries", out var slow))
                    table.AddRow("Slow Queries", int.TryParse(slow, out var s) && s > 0 ? $"[red]{slow}[/]" : $"[green]{slow}[/]");
                    
                if (extraMetrics.TryGetValue("Threads_running", out var threads))
                    table.AddRow("Threads Running", $"[yellow]{threads}[/]");

                if (extraMetrics.TryGetValue("Bytes_received", out var recv) && long.TryParse(recv, out var recvBytes))
                    table.AddRow("Network Received", $"[blue]{recvBytes / 1024.0 / 1024.0:N2} MB[/]");

                if (extraMetrics.TryGetValue("Bytes_sent", out var sent) && long.TryParse(sent, out var sentBytes))
                    table.AddRow("Network Sent", $"[blue]{sentBytes / 1024.0 / 1024.0:N2} MB[/]");

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

    private static async Task ShowStatisticsChartAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Analyzing database structure and sizes...", async ctx =>
            {
                var tables = await monitor.GetTableSizesAsync(dbName);
                
                if (!tables.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tables found to analyze.[/]");
                    return;
                }

                AnsiConsole.Write(new Rule($"[bold cyan]Database Statistics: {dbName}[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.WriteLine();

                // 1. Overall Database Breakdown (Data vs Indexes)
                long totalData = tables.Sum(t => t.DataLength);
                long totalIndexes = tables.Sum(t => t.IndexLength);
                long totalSize = tables.Sum(t => t.SizeBytes);

                AnsiConsole.MarkupLine("[bold yellow]1. Storage Distribution[/]");
                var breakdown = new BreakdownChart()
                    .Width(60)
                    .AddItem("Raw Data", totalData, Color.Green)
                    .AddItem("Indexes", totalIndexes, Color.Blue);
                AnsiConsole.Write(breakdown);
                AnsiConsole.WriteLine();

                // 2. Top Largest Tables Bar Chart
                AnsiConsole.MarkupLine("[bold yellow]2. Top Largest Tables (by Total Size)[/]");
                var topTables = tables.OrderByDescending(t => t.SizeBytes).Take(10).ToList();

                var chart = new BarChart()
                    .Width(60)
                    .Label("[grey]Size in KB[/]")
                    .CenterLabel();

                var colors = new[] { Color.Red, Color.Orange1, Color.Yellow, Color.Green, Color.Blue, Color.Purple, Color.Magenta1 };
                int colorIndex = 0;

                foreach (var t in topTables)
                {
                    double sizeKb = t.SizeBytes / 1024.0;
                    chart.AddItem(Markup.Escape(t.TableName), Math.Round(sizeKb, 2), colors[colorIndex % colors.Length]);
                    colorIndex++;
                }

                AnsiConsole.Write(chart);
                AnsiConsole.WriteLine();

                // 3. Table Density (Average row size simulation based on structure)
                AnsiConsole.MarkupLine("[bold yellow]3. Summary[/]");
                var grid = new Grid()
                    .AddColumn(new GridColumn().NoWrap().PadRight(4))
                    .AddColumn();

                grid.AddRow("[grey]Total Tables:[/]", $"[white]{tables.Count()}[/]");
                grid.AddRow("[grey]Total Size:[/]", $"[white]{totalSize / 1024.0 / 1024.0:N2} MB[/]");
                grid.AddRow("[grey]Heaviest Table:[/]", $"[white]{Markup.Escape(topTables.FirstOrDefault()?.TableName ?? "N/A")}[/]");

                AnsiConsole.Write(new Panel(grid).Expand().BorderColor(Color.Grey));
            });
    }
}