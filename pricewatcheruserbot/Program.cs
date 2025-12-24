using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using pricewatcheruserbot.UserInputProvider;
using pricewatcheruserbot.UserInputProvider.Impl;
using TL;
using Channel = System.Threading.Channels.Channel;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton(provider =>
{
    var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();
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
    
    return client;
});
builder.Services.AddKeyedSingleton("ozon", (provider, _) => (Func<string, Task<string>>)(async what =>
    {
        var userInputProvider = provider.GetRequiredService<IUserInputProvider>();

        return what switch
        {
            "phone_number" => await userInputProvider.Ozon_GetPhoneNumber(),
            "phone_verification_code" => await userInputProvider.Ozon_GetPhoneVerificationCode(),
            "email_verification_code" => await userInputProvider.Ozon_GetEmailVerificationCode(),
            _ => throw new InvalidOperationException()
        };
    })
);

builder.Services.AddDbContext<AppDbContext>(x =>
{
    x.UseSqlite(EnvironmentVariables.DbConnectionString);
});
builder.Services.AddMemoryCache();

builder.Services.AddScoped<AddCommand.Handler>();
builder.Services.AddScoped<ListCommand.Handler>();
builder.Services.AddScoped<RemoveCommand.Handler>();

builder.Services.AddSingleton<IScrapper, OzonScrapper>();
builder.Services.AddSingleton<IScrapper, WildberriesScrapper>();
builder.Services.AddSingleton<IScrapper, YandexMarketScrapper>();

builder.Services.AddSingleton<BrowserService>();
builder.Services.AddSingleton<ScrapperFactory>();

var channel = Channel.CreateBounded<WorkerItem>(
    new BoundedChannelOptions(32)
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.DropWrite
    });
builder.Services.AddSingleton<ChannelReader<WorkerItem>>(channel);
builder.Services.AddSingleton<ChannelWriter<WorkerItem>>(resolver =>
{
    var hostLifetime = resolver.GetRequiredService<IHostApplicationLifetime>();
    
    hostLifetime.ApplicationStopping.Register(() => { channel.Writer.Complete(); });
    
    return channel;
});

builder.Services.AddHostedService<ProducerWorker>();
builder.Services.AddHostedService<ConsumerWorker>();

if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
    DotNetEnv.Env.TraversePath();
    
    builder.Services.AddSingleton<IUserInputProvider, EnvUserInputProvider>();
}
else
{
    builder.Services.AddSingleton<IUserInputProvider, FileUserInputProvider>();
}

using (var host = builder.Build())
{
    var client = host.Services.GetRequiredService<WTelegram.Client>();
    var updateRouter = host.Services.GetRequiredService<UpdateHandler>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var scrappers = host.Services.GetServices<IScrapper>();
    var userInputProvider = host.Services.GetRequiredService<IUserInputProvider>();
    
    _ = client.WithUpdateManager(updateRouter.Handle);
    await DoLogin(await userInputProvider.Telegram_GetPhoneNumber());
    
    async Task DoLogin(string loginInfo)
    {
        while (client.User is null)
        {
            loginInfo = await client.Login(loginInfo) switch
            {
                "password" => await userInputProvider.Telegram_GetPassword(),
                "verification_code" => await userInputProvider.Telegram_GetVerificationCode(),
                _ => null!
            };
        }
    }
    
    logger.LogInformation("Telegram session saved");
    
    try
    { 
        foreach (var scrapper in scrappers)
        {
            await scrapper.Authorize();
        }

        logger.LogInformation("Successful authorization in services");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed authorization in services");
    }

    await using (var scope = host.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }

    await host.RunAsync();
}