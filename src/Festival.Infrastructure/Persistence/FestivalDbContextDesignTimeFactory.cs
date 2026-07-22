using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Festival.Infrastructure.Persistence;

internal sealed class FestivalDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<FestivalDbContext>
{
    public FestivalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=festival_design_time;Username=festival;Password=festival")
            .Options;

        return new FestivalDbContext(options);
    }
}
