using Microsoft.EntityFrameworkCore;
using nure_api.Models;

namespace nure_api;

public class Context : DbContext
{
    public DbSet<Group> Groups { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Auditory> Auditories { get; set; }
    /*public Context() => Database.EnsureCreatedAsync();*/
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            File.ReadAllText("dbConnection"));
    }
    
    /*protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>().Property(m => m.Schedule).Is;

        base.OnModelCreating(modelBuilder);
    }*/
}
