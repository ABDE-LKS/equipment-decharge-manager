using EquipmentDechargeManager.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Services;

public static class DatabaseInitializer
{
    public static string ConnectionString { get; set; } =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "Host=127.0.0.1;Port=5435;Database=dechargedb;Username=decharge_user;Password=decharge_password";

    public static DechargeDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DechargeDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString)
                      .UseSnakeCaseNamingConvention();

        return new DechargeDbContext(optionsBuilder.Options);
    }

    public static async Task InitializeAsync()
    {
        try
        {
            using var db = CreateDbContext();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Migration failed or skipped: {ex.Message}");
        }
    }
}
