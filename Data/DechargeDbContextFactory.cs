using EquipmentDechargeManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentDechargeManager.Data;

public class DechargeDbContextFactory : IDesignTimeDbContextFactory<DechargeDbContext>
{
    public DechargeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DechargeDbContext>();

        optionsBuilder.UseNpgsql(DatabaseConfiguration.GetConnectionString())
                      .UseSnakeCaseNamingConvention();

        return new DechargeDbContext(optionsBuilder.Options);
    }
}
