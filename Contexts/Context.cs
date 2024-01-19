using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nure_api.Models;

namespace nure_api;

public class Context : IdentityDbContext
{
    public DbSet<Group> Groups { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Auditory> Auditories { get; set; }
    public Context() => Database.EnsureCreated();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            File.ReadAllText("dbConnection"));
    }
    
    public Context(DbContextOptions<Context> options) : base(options) { }
}
