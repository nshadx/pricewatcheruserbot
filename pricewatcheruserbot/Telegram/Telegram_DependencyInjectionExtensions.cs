using pricewatcheruserbot.Configuration;
using TL;

namespace pricewatcheruserbot.Telegram;

public static class Telegram_DependencyInjectionExtensions
{
    public static IServiceCollection AddMessageServices(this IServiceCollection services)
    {
        services.AddSingleton<MessageSender>();
        services.AddScoped<MessageManager>();

        return services;
    }
    
    public static IServiceCollection AddTelegram(this IServiceCollection services)
    {
        services.AddSingleton<TelegramService>();
        services.AddSingleton<UpdateHandler>();
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();
            var userInputProvider = provider.GetRequiredService<IUserInputProvider>();
            var updateHandler = provider.GetRequiredService<UpdateHandler>();
            
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
            
            _ = client.WithUpdateManager(updateHandler.Handle);
    
            return client;
        });

        return services;
    }
}