using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Services;

public static class Services_DependencyInjectionExtensions
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(x =>
        {
            x.UseSqlite(EnvironmentVariables.DbConnectionString);
        });

        return services;
    }

    public static IServiceCollection AddTrackerServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<WorkerItemTracker>();

        return services;
    }
}