using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Persistence;

public sealed class FestivalDbContext(
    DbContextOptions<FestivalDbContext> options)
    : DbContext(options);
