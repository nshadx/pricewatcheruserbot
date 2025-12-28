using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot.Services;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkerItem> WorkerItems { get; set; }
    public DbSet<SentMessage> SentMessages { get; set; }
    public DbSet<UserAgent> UserAgents { get; set; }
}