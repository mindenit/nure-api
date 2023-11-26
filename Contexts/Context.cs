using Microsoft.EntityFrameworkCore;
using nure_api.Models;

namespace nure_api;

public class Context : DbContext
{
    public DbSet<Group> Groups { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Auditory> Auditories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            File.ReadAllText("dbConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Додайте логіку для перевірки наявності таблиць перед викликом EnsureCreatedAsync()
        if (!modelBuilder.Model.GetEntityTypes().Any())
        {
            Database.EnsureCreatedAsync();
        }

        // Додайте інші налаштування моделі

        base.OnModelCreating(modelBuilder);
    }
}

