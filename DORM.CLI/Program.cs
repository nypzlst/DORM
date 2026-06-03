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
            AnsiConsole.MarkupLine("[red]Помилка:[/] Відсутня змінна CONNECTION_STRING або змінні DORM_* у файлі .env.");
            AnsiConsole.MarkupLine("[yellow]Підказка:[/] Переконайтеся, що файл .env містить CONNECTION_STRING=... або DORM_HOST, DORM_USER, DORM_PASS, DORM_DB.");
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

        AnsiConsole.MarkupLine($"[grey]Підключено до:[/] {dbType.ToUpper()} | [grey]База даних:[/] {dbName}");
        AnsiConsole.WriteLine();

        while (true)
        {
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Виберіть дію:")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "Статус сервера", 
                        "Список таблиць", 
                        "Статистика БД",
                        "Видалити таблицю (Drop)", 
                        "Очистити таблицю (Truncate)", 
                        "Вихід"
                    }));

            try
            {
                switch (action)
                {
                    case "Статус сервера":
                        await ShowStatusAsync(monitor, dbName);
                        break;
                    case "Список таблиць":
                        await ShowTablesAsync(monitor, dbName);
                        break;
                    case "Статистика БД":
                        await ShowStatisticsChartAsync(monitor, dbName);
                        break;
                    case "Видалити таблицю (Drop)":
                        await DropTablePromptAsync(admin, monitor, dbName);
                        break;
                    case "Очистити таблицю (Truncate)":
                        await TruncateTablePromptAsync(admin, monitor, dbName);
                        break;
                    case "Вихід":
                        return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Натисніть Enter для продовження...[/]");
            Console.ReadLine();
            AnsiConsole.Clear();
        }
    }

    private static async Task ShowStatusAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Отримання статусу сервера...", async ctx =>
            {
                var version = await monitor.GetServerVersionAsync();
                var connections = await monitor.GetActiveConnectionsAsync();
                var dbSize = await monitor.GetDatabaseSizeAsync(dbName);
                var extraMetrics = await monitor.GetServerStatusAsync();

                var table = new Table();
                table.AddColumn("Метрика");
                table.AddColumn("Значення");

                table.AddRow("Версія сервера", $"[green]{version}[/]");
                table.AddRow("Розмір БД (Байт)", $"[blue]{dbSize:N0}[/]");
                table.AddRow("Активні підключення", $"[yellow]{connections}[/]");
                
                if (extraMetrics.TryGetValue("Uptime", out var uptimeStr) && long.TryParse(uptimeStr, out var uptimeSeconds))
                {
                    var timeSpan = TimeSpan.FromSeconds(uptimeSeconds);
                    table.AddRow("Час роботи (Uptime)", $"[cyan]{timeSpan.Days}д {timeSpan.Hours}г {timeSpan.Minutes}хв[/]");
                }
                
                if (extraMetrics.TryGetValue("Questions", out var questions))
                    table.AddRow("Всього запитів", $"[magenta]{questions}[/]");
                    
                if (extraMetrics.TryGetValue("Slow_queries", out var slow))
                    table.AddRow("Повільні запити", int.TryParse(slow, out var s) && s > 0 ? $"[red]{slow}[/]" : $"[green]{slow}[/]");
                    
                if (extraMetrics.TryGetValue("Threads_running", out var threads))
                    table.AddRow("Активні потоки", $"[yellow]{threads}[/]");

                if (extraMetrics.TryGetValue("Bytes_received", out var recv) && long.TryParse(recv, out var recvBytes))
                    table.AddRow("Отримано по мережі", $"[blue]{recvBytes / 1024.0 / 1024.0:N2} МБ[/]");

                if (extraMetrics.TryGetValue("Bytes_sent", out var sent) && long.TryParse(sent, out var sentBytes))
                    table.AddRow("Відправлено по мережі", $"[blue]{sentBytes / 1024.0 / 1024.0:N2} МБ[/]");

                AnsiConsole.Write(table);
            });
    }

    private static async Task ShowTablesAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Отримання списку таблиць...", async ctx =>
            {
                var tables = await monitor.GetTableSizesAsync(dbName);

                var table = new Table();
                table.AddColumn("Назва таблиці");
                table.AddColumn("Розмір (Байт)");

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
            AnsiConsole.MarkupLine("[yellow]Таблиць не знайдено.[/]");
            return;
        }

        var tableToDrop = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Виберіть таблицю для [red]ВИДАЛЕННЯ (DROP)[/]:")
                .AddChoices(tables.Append("<Скасувати>")));

        if (tableToDrop == "<Скасувати>") return;

        if (AnsiConsole.Confirm($"Ви впевнені, що хочете безповоротно видалити таблицю [red]{Markup.Escape(tableToDrop)}[/]?", defaultValue: false))
        {
            await admin.DropTableAsync(tableToDrop);
            AnsiConsole.MarkupLine($"[green]Таблицю {Markup.Escape(tableToDrop)} успішно видалено.[/]");
        }
    }

    private static async Task TruncateTablePromptAsync(IDatabaseAdmin admin, IDatabaseMonitor monitor, string dbName)
    {
         var tables = await monitor.GetTablesAsync(dbName);
        if (!tables.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Таблиць не знайдено.[/]");
            return;
        }

        var tableToTruncate = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Виберіть таблицю для [yellow]ОЧИЩЕННЯ (TRUNCATE)[/]:")
                .AddChoices(tables.Append("<Скасувати>")));

        if (tableToTruncate == "<Скасувати>") return;

        if (AnsiConsole.Confirm($"Ви впевнені, що хочете очистити дані з таблиці [yellow]{Markup.Escape(tableToTruncate)}[/]?", defaultValue: false))
        {
            await admin.TruncateTableAsync(tableToTruncate);
            AnsiConsole.MarkupLine($"[green]Таблицю {Markup.Escape(tableToTruncate)} успішно очищено.[/]");
        }
    }

    private static async Task ShowStatisticsChartAsync(IDatabaseMonitor monitor, string dbName)
    {
        await AnsiConsole.Status()
            .StartAsync("Аналіз структури та розмірів бази даних...", async ctx =>
            {
                var tables = await monitor.GetTableSizesAsync(dbName);
                
                if (!tables.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]Не знайдено таблиць для аналізу.[/]");
                    return;
                }

                AnsiConsole.Write(new Rule($"[bold cyan]Статистика бази даних: {dbName}[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.WriteLine();

                // 1. Overall Database Breakdown (Data vs Indexes)
                long totalData = tables.Sum(t => t.DataLength);
                long totalIndexes = tables.Sum(t => t.IndexLength);
                long totalSize = tables.Sum(t => t.SizeBytes);

                AnsiConsole.MarkupLine("[bold yellow]1. Розподіл пам'яті[/]");
                var breakdown = new BreakdownChart()
                    .Width(60)
                    .AddItem("Сирі дані", totalData, Color.Green)
                    .AddItem("Індекси", totalIndexes, Color.Blue);
                AnsiConsole.Write(breakdown);
                AnsiConsole.WriteLine();

                // 2. Top Largest Tables Bar Chart
                AnsiConsole.MarkupLine("[bold yellow]2. Найбільші таблиці (за загальним розміром)[/]");
                var topTables = tables.OrderByDescending(t => t.SizeBytes).Take(10).ToList();

                var chart = new BarChart()
                    .Width(60)
                    .Label("[grey]Розмір у КБ[/]")
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
                AnsiConsole.MarkupLine("[bold yellow]3. Загальний підсумок[/]");
                var grid = new Grid()
                    .AddColumn(new GridColumn().NoWrap().PadRight(4))
                    .AddColumn();

                grid.AddRow("[grey]Всього таблиць:[/]", $"[white]{tables.Count()}[/]");
                grid.AddRow("[grey]Загальний розмір:[/]", $"[white]{totalSize / 1024.0 / 1024.0:N2} МБ[/]");
                grid.AddRow("[grey]Найважча таблиця:[/]", $"[white]{Markup.Escape(topTables.FirstOrDefault()?.TableName ?? "Немає")}[/]");

                AnsiConsole.Write(new Panel(grid).Expand().BorderColor(Color.Grey));
            });
    }
}