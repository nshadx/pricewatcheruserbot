using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Configuration;
using TL;

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
    
    public static IServiceCollection AddMessageServices(this IServiceCollection services)
    {
        services.AddSingleton<MessageSender>();
        services.AddScoped<MessageManager>();

        return services;
    }
    
    public static IServiceCollection AddTelegramClient(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();
            var updateHandlerLogger = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var userInputProvider = provider.GetRequiredService<IUserInputProvider>();
            
            WTelegram.Helpers.Log = (logLevel, message) =>
            {
#pragma warning disable CA2254
                logger.Log((LogLevel)(logLevel - 1), message);
#pragma warning restore CA2254
            };

            var client = new WTelegram.Client(
                userInputProvider.Telegram_GetApiId().Result,
                userInputProvider.Telegram_GetApiHash().Result,
                EnvironmentVariables.TelegramSessionFilePath
            );

            UpdateHandler handler = new(provider, updateHandlerLogger, client);

            _ = client.WithUpdateManager(handler.Handle);
    
            return client;
        });

        return services;
    }
}