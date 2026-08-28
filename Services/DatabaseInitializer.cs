using EquipmentDechargeManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Services;

public sealed class DatabaseInitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SetupInstructions { get; init; }
}

public static class DatabaseInitializer
{
    public static string ConnectionString => DatabaseConfiguration.GetConnectionString();

    public static DechargeDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DechargeDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString)
                      .UseSnakeCaseNamingConvention()
                      .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new DechargeDbContext(optionsBuilder.Options);
    }

    public static async Task<DatabaseInitResult> InitializeAsync()
    {
        try
        {
            var settings = DatabaseConfiguration.GetSettings();
            var connectionString = DatabaseConfiguration.GetConnectionString();

            // Wait for PostgreSQL server to become reachable (short retry loop).
            var reachable = false;
            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (await IsServerReachableAsync(settings))
                {
                    reachable = true;
                    break;
                }

                await Task.Delay(1000);
            }

            if (!reachable)
            {
                return Failure(
                    $"Cannot connect to PostgreSQL at {settings.Host}:{settings.Port}. " +
                    "Make sure PostgreSQL is installed and the service is running.");
            }

            // Create the database if it doesn't exist yet.
            if (!await DatabaseExistsAsync(settings))
            {
                try
                {
                    await CreateDatabaseAsync(settings);
                }
                catch (PostgresException ex) when (ex.SqlState == "42P04") // duplicate_database
                {
                    // Another process created the database concurrently; continue.
                }
            }

            using var db = CreateDbContext();
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pending model changes", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureCreatedAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("The model for context", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureCreatedAsync();
            }

            // Normalize existing database records to strict two-state values: ACTIVE and RETOURNÉE
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE decharges SET status = 'RETOURNÉE' WHERE status NOT IN ('ACTIVE', 'RETOURNÉE');");

            // Resynchronize PostgreSQL primary key sequences to prevent duplicate key conflicts if data was seeded/imported
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    SELECT setval(pg_get_serial_sequence('decharges', 'id'), COALESCE((SELECT MAX(id) FROM decharges), 1));
                    SELECT setval(pg_get_serial_sequence('decharge_items', 'id'), COALESCE((SELECT MAX(id) FROM decharge_items), 1));
                    SELECT setval(pg_get_serial_sequence('employees', 'id'), COALESCE((SELECT MAX(id) FROM employees), 1));
                    SELECT setval(pg_get_serial_sequence('equipments', 'id'), COALESCE((SELECT MAX(id) FROM equipments), 1));
                ");
            }
            catch { }

            return new DatabaseInitResult { Success = true };
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            return Failure("PostgreSQL authentication failed. Check the username and password in appsettings.json.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            var settings = DatabaseConfiguration.GetSettings();
            return Failure(
                $"Database '{settings.Database}' does not exist. " +
                $"Create it with: CREATE DATABASE {settings.Database};");
        }
        catch (NpgsqlException ex)
        {
            return Failure($"PostgreSQL connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Failure($"Database initialization failed: {ex.Message}");
        }
    }

    private static async Task<bool> IsServerReachableAsync(DatabaseSettings settings)
    {
        try
        {
            await using var connection = new NpgsqlConnection(DatabaseConfiguration.GetAdminConnectionString());
            await connection.OpenAsync();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            throw new InvalidOperationException(
                "PostgreSQL authentication failed. Check the username and password in appsettings.json.");
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private static async Task<bool> DatabaseExistsAsync(DatabaseSettings settings)
    {
        await using var connection = new NpgsqlConnection(DatabaseConfiguration.GetAdminConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
        command.Parameters.AddWithValue("databaseName", settings.Database);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private static async Task CreateDatabaseAsync(DatabaseSettings settings)
    {
        await using var connection = new NpgsqlConnection(DatabaseConfiguration.GetAdminConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        // Quote identifiers to avoid SQL injection via database/username values
        var dbName = settings.Database.Replace("\"", "\"\"");
        var owner = settings.Username.Replace("\"", "\"\"");
        command.CommandText = $"CREATE DATABASE \"{dbName}\" OWNER \"{owner}\"";
        await command.ExecuteNonQueryAsync();
    }

    private static DatabaseInitResult Failure(string message)
    {
        return new DatabaseInitResult
        {
            Success = false,
            ErrorMessage = message,
            SetupInstructions = DatabaseConfiguration.GetSetupInstructions()
        };
    }
}
