using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentDechargeManager.Data;

public class DechargeDbContextFactory : IDesignTimeDbContextFactory<DechargeDbContext>
{
    public DechargeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DechargeDbContext>();

        string connectionString = System.Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=127.0.0.1;Port=5435;Database=dechargedb;Username=decharge_user;Password=decharge_password";

        optionsBuilder.UseNpgsql(connectionString)
                      .UseSnakeCaseNamingConvention();

        return new DechargeDbContext(optionsBuilder.Options);
    }
}
