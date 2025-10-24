using Microsoft.EntityFrameworkCore;
using EbarimtCheckerService.Models;

namespace EbarimtCheckerService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
}
