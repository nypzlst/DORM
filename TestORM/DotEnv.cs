namespace TestORM;

/// <summary>
/// Мінімальний завантажувач .env-файлів: KEY=VALUE по рядках, без підстановок і складного екранування.
/// Підтримує: порожні рядки, коментарі (#...), значення у "..." та '...', BOM на початку файлу.
/// Уже виставлені змінні оточення процесу не перезаписуються.
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

            // Підтримуємо нотацію `export KEY=VALUE`.
            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line.Substring("export ".Length).TrimStart();

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line.Substring(0, eq).Trim();
            var value = line.Substring(eq + 1).Trim();

            // Знімаємо парні лапки.
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
    /// Піднімаємося вгору від поточної директорії і від каталогу з бінарником, шукаємо .env.
    /// Корисно при запуску з bin/Debug/...
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
