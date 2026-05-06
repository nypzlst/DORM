namespace TestORM;

/// <summary>
/// Минимальный загрузчик .env-файлов: KEY=VALUE по строкам, без подстановок и сложного экранирования.
/// Поддерживает: пустые строки, комментарии (#...), значения в "..." и '...', BOM в начале файла.
/// Уже выставленные переменные окружения процесса не перезаписываются.
/// </summary>
internal static class DotEnv
{
    public static bool Load(string? path = null)
    {
        path ??= FindDefaultPath();
        if (path is null || !File.Exists(path)) return false;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            // Поддерживаем нотацию `export KEY=VALUE`.
            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line.Substring("export ".Length).TrimStart();

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line.Substring(0, eq).Trim();
            var value = line.Substring(eq + 1).Trim();

            // Снимаем парные кавычки.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
        return true;
    }

    /// <summary>
    /// Поднимаемся вверх от текущей директории и от каталога с бинарём, ищем .env.
    /// Полезно при запуске из bin/Debug/...
    /// </summary>
    private static string? FindDefaultPath()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
        }
        return null;
    }
}
