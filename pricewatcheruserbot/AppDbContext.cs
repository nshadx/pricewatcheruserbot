using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkerItem> WorkerItems { get; set; }
    public DbSet<SentMessage> SentMessages { get; set; }
    public DbSet<UserAgent> UserAgents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UserAgent fallbackUserAgent = new()
        {
            Id = 1,
            Value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };
        modelBuilder.Entity<UserAgent>().HasData(fallbackUserAgent);
        
        base.OnModelCreating(modelBuilder);
    }
}