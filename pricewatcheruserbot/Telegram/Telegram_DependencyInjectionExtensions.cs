using Microsoft.Extensions.Options;
using TL;

namespace pricewatcheruserbot.Telegram;

public static class Telegram_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddMessageServices()
        {
            builder.Services.AddSingleton<MessageSender>();
            builder.Services.AddScoped<MessageManager>();

            return builder;
        }
    
        public IHostApplicationBuilder AddTelegram()
        {
            builder.Services.AddSingleton<TelegramInput>();
            builder.Services.Configure<TelegramConfiguration>(builder.Configuration.GetRequiredSection(nameof(TelegramConfiguration)));
            builder.Services.AddSingleton<TelegramService>();
            builder.Services.AddSingleton<UpdateHandler>();
            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();
                var updateHandler = provider.GetRequiredService<UpdateHandler>();
                var configuration = provider.GetRequiredService<IOptions<TelegramConfiguration>>().Value;
            
                WTelegram.Helpers.Log = (logLevel, message) =>
                {
#pragma warning disable CA2254
                    logger.Log((LogLevel)(logLevel - 1), message);
#pragma warning restore CA2254
                };

                var client = new WTelegram.Client(
                    configuration.ApiId,
                    configuration.ApiHash,
                    configuration.SessionFilePath
                );
            
                _ = client.WithUpdateManager(updateHandler.Handle);
    
                return client;
            });

            return builder;
        }
    }
}