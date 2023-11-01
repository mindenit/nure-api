using Microsoft.EntityFrameworkCore;
using nure_api.Models;

namespace nure_api;

public class GroupContext : DbContext
{
    public DbSet<Group> Groups { get; set; }
    public GroupContext() => Database.EnsureCreated();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            File.ReadAllText("dbConnection"),
            new MySqlServerVersion(new Version(10, 6, 15)))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);;
    }
}
