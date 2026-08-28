using System;
using System.IO;
using System.Text.Json;

namespace EquipmentDechargeManager.Services;

public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "equipment_decharge_manager";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "";
}

public static class DatabaseConfiguration
{
    public const string ConfigFileName = "appsettings.json";
    public const string ExampleConfigFileName = "appsettings.example.json";

    private static DatabaseSettings? _cachedSettings;

    public static DatabaseSettings GetSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            _cachedSettings = ParseConnectionString(databaseUrl);
            return _cachedSettings;
        }

        var fileSettings = LoadFromConfigFile();
        if (fileSettings != null)
        {
            _cachedSettings = fileSettings;
            return _cachedSettings;
        }

        _cachedSettings = new DatabaseSettings();
        return _cachedSettings;
    }

    public static string BuildConnectionString(DatabaseSettings settings)
    {
        return $"Host={settings.Host};Port={settings.Port};Database={settings.Database};Username={settings.Username};Password={settings.Password};Include Error Detail=true";
    }

    public static string GetConnectionString()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return databaseUrl;

        var settings = GetSettings();

        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "PostgreSQL password is not configured. " +
                $"Copy {ExampleConfigFileName} to {ConfigFileName}, set your password, " +
                "or set the DATABASE_URL environment variable.");
        }

        return BuildConnectionString(settings);
    }

    public static string GetAdminConnectionString()
    {
        var settings = GetSettings();
        return $"Host={settings.Host};Port={settings.Port};Database=postgres;Username={settings.Username};Password={settings.Password}";
    }

    public static string? FindConfigFilePath()
    {
        foreach (var directory in GetSearchDirectories())
        {
            var path = Path.Combine(directory, ConfigFileName);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    public static string GetSetupInstructions()
    {
        var settings = GetSettings();
        return $"""
            Local PostgreSQL Setup — Windows

            1. Install PostgreSQL 16+ from https://www.postgresql.org/download/windows/
                2. Configure the application:
                    - Copy appsettings.example.json to appsettings.json in the project folder
                    - Set Host, Port, Database, Username, and Password

                    Or set the DATABASE_URL environment variable:
                    Host=localhost;Port=5432;Database={settings.Database};Username=postgres;Password=YOUR_PASSWORD

                3. Start the application. On first run the application will create the database
                    (if missing) and apply EF Core migrations automatically.

                4. Verify PostgreSQL is running:
                    Get-Service postgresql*
                    Or connect with pgAdmin to localhost:{settings.Port}

                5. Start the application:
                    dotnet run
            """;
    }

    private static DatabaseSettings? LoadFromConfigFile()
    {
        foreach (var directory in GetSearchDirectories())
        {
            var path = Path.Combine(directory, ConfigFileName);
            if (!File.Exists(path))
                continue;

            try
            {
                var json = File.ReadAllText(path);
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("Database", out var databaseElement))
                    continue;

                return JsonSerializer.Deserialize<DatabaseSettings>(databaseElement.GetRawText())
                    ?? new DatabaseSettings();
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private static DatabaseSettings ParseConnectionString(string connectionString)
    {
        var settings = new DatabaseSettings();
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = part[..separatorIndex].Trim();
            var value = part[(separatorIndex + 1)..].Trim();

            switch (key.ToLowerInvariant())
            {
                case "host":
                    settings.Host = value;
                    break;
                case "port":
                    if (int.TryParse(value, out var port))
                        settings.Port = port;
                    break;
                case "database":
                    settings.Database = value;
                    break;
                case "username":
                case "user id":
                case "userid":
                    settings.Username = value;
                    break;
                case "password":
                    settings.Password = value;
                    break;
            }
        }

        return settings;
    }

    private static string[] GetSearchDirectories()
    {
        var directories = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EquipmentDechargeManager")
        };

        var expanded = new System.Collections.Generic.List<string>();
        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            expanded.Add(directory);

            var parent = Directory.GetParent(directory);
            for (var depth = 0; parent != null && depth < 3; depth++)
            {
                expanded.Add(parent.FullName);
                parent = parent.Parent;
            }
        }

        return expanded.ToArray();
    }
}
