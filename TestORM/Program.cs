using System.Text;
using DORM.Infrastructure.Core;
using DORM.Providers.MySQL;
using TestORM;
using TestORM.Example;

// Windows-консоль за замовчуванням використовує cp866/cp1251, через що
// українські 'і', 'є', 'ї' виводяться як '?'. Перемикаємо вивід на UTF-8.
Console.OutputEncoding = Encoding.UTF8;

// ─── Налаштування підключення ───────────────────────────────────────────────
// Конфіг читається в такому порядку (з пріоритетом змінних оточення процесу):
//   1) змінні оточення, виставлені ззовні;
//   2) файл .env у корені репозиторію або поряд з бінарником (див. .env.example).
// Підтримуються:
//   DORM_CONN_FILE — шлях до файлу з рядком підключення, АБО
//   DORM_DB / DORM_HOST / DORM_USER / DORM_PASS / DORM_PORT — окремо.
// ─────────────────────────────────────────────────────────────────────────────
DotEnv.Load();

Database db;
var connFile = Environment.GetEnvironmentVariable("DORM_CONN_FILE");
if (!string.IsNullOrWhiteSpace(connFile))
{
    db = new Database(connFile);
}
else
{
    var dbName  = Environment.GetEnvironmentVariable("DORM_DB")   ?? "railway";
    var host    = Environment.GetEnvironmentVariable("DORM_HOST") ?? "localhost";
    var dbUser  = Environment.GetEnvironmentVariable("DORM_USER") ?? "root";
    var dbPass  = Environment.GetEnvironmentVariable("DORM_PASS") ?? "";
    var port    = int.TryParse(Environment.GetEnvironmentVariable("DORM_PORT"), out var p) ? p : 3306;
    db = new Database(dbName, host, dbUser, dbPass, port);
}

var crud    = new MySqlCrudQuery();
var context = new DormContext(db, crud);

void PrintUsers()
{
    var users = db.SelectQuery<User>("SELECT * FROM TUser");
    if (users.Count == 0)
    {
        Console.WriteLine("  (таблиця порожня)");
        return;
    }
    foreach (var u in users)
        Console.WriteLine($"  Id={u.Id}, Email={u.Email}");
}

// 1. CREATE TABLE SQL
Console.WriteLine("=== CREATE TABLE SQL ===");
string createUserSql    = crud.CreateTable(new User());
string createCatalogSql = crud.CreateTable(new Catalog());
Console.WriteLine(createUserSql);
Console.WriteLine(createCatalogSql);

// 2. CONNECTION
Console.WriteLine("\n=== CONNECTION TEST ===");
try
{
    await db.CheckConnection();
    Console.WriteLine("OK — підключення успішне");
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL — {ex.Message}");
    return;
}

// 3. CREATE TABLE
Console.WriteLine("\n=== CREATE TABLE ===");
try
{
    db.ExecuteRaw(createUserSql);
    Console.WriteLine("OK — TUser створена");
    db.ExecuteRaw(createCatalogSql);
    Console.WriteLine("OK — Catalog створена");
}
catch (Exception ex)
{
    Console.WriteLine($"INFO — {ex.Message}");
}

// 4. INSERT (батч)
const int INSERT_COUNT = 20;
const int UPDATE_COUNT = 5;
// Унікальний суфікс на запуск, щоб не впертись у UNIQUE(Email) при повторі.
var runTag = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

Console.WriteLine($"\n=== INSERT TEST (×{INSERT_COUNT}) ===");
for (int i = 1; i <= INSERT_COUNT; i++)
{
    context.Insert(new User { Email = $"user_{runTag}_{i:D3}@example.com" });
}
try
{
    await context.SaveChanges();
    Console.WriteLine($"OK — INSERT виконано ({INSERT_COUNT} рядків)");
    Console.WriteLine("Таблиця після INSERT:");
    PrintUsers();
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL — {ex.Message}");
}

// 5. UPDATE
// Демонстраційний сценарій: беремо щойно вставлених юзерів з email
// "user_<runTag>_NNN@example.com" і апдейтом прибираємо з пошти дату —
// має вийти "user_NNN@example.com". У логах це явно видно як «було → стало».
Console.WriteLine($"\n=== UPDATE TEST (×{UPDATE_COUNT}) ===");

var pendingUpdates = new List<(int Id, string OldEmail, string NewEmail)>();

for (int i = 1; i <= UPDATE_COUNT; i++)
{
    var oldEmail = $"user_{runTag}_{i:D3}@example.com";
    var newEmail = $"user_{i:D3}@example.com";

    var target = db.SelectQuery<User>($"SELECT * FROM TUser WHERE Email = '{oldEmail}'")
                   .FirstOrDefault();

    if (target is null)
    {
        Console.WriteLine($"SKIP #{i:D3} — не знайшли юзера з Email='{oldEmail}'");
        continue;
    }

    Console.WriteLine($"ДО    #{i:D3}: Id={target.Id}, Email={target.Email}");
    Console.WriteLine($"ХОЧУ  #{i:D3}: Id={target.Id}, Email={newEmail}   (прибираємо дату з пошти)");

    context.Update(new User { Id = target.Id, Email = newEmail });
    pendingUpdates.Add((target.Id, target.Email!, newEmail));
}

if (pendingUpdates.Count == 0)
{
    Console.WriteLine("SKIP — немає рядків для оновлення");
}
else
{
    try
    {
        await context.SaveChanges();
        Console.WriteLine($"OK — UPDATE виконано ({pendingUpdates.Count} рядків одним батчем)");

        foreach (var (id, oldE, expectedNewE) in pendingUpdates)
        {
            var after = db.SelectQuery<User>($"SELECT * FROM TUser WHERE Id = {id}")
                          .FirstOrDefault();
            Console.WriteLine($"ПІСЛЯ Id={id}: Email '{oldE}' → '{after?.Email}'"
                              + (after?.Email == expectedNewE ? "" : "   [!!! не співпало з очікуваним]"));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL — {ex.Message}");
    }
}
