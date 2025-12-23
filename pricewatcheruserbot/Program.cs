using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using TL;
using Channel = System.Threading.Channels.Channel;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton(provider =>
{
    var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();

    var session = File.Open(EnvironmentVariables.TelegramSessionFilePath, FileMode.OpenOrCreate);

    Func<string, string?> config = what =>
    {
        switch (what)
        {
            case "api_id": Console.Write("(Telegram) Enter ApiId: "); return Console.ReadLine();
            case "api_hash": Console.Write("(Telegram) ApiHash: "); return Console.ReadLine();
            case "password": Console.Write("(Telegram) Password: "); return Console.ReadLine();
            case "phone_number": Console.Write("(Telegram) Phone number with country code (+7): "); return Console.ReadLine();
            case "verification_code": Console.Write("(Telegram) Verification code: "); return Console.ReadLine();
            default: return null;
        }
    };
    
    WTelegram.Helpers.Log = (logLevel, message) =>
    {
        var level = (LogLevel)logLevel;

        if (level is LogLevel.Error or LogLevel.Critical)
        {
            logger.Log(level, message);
        }
    };
    
    var client = new WTelegram.Client(
        config,
        session
    );
    
    return client;
});
builder.Services.AddKeyedSingleton("ozon", (Func<string, string>)(what =>
    {
        switch (what)
        {
            case "phone_number": Console.Write("(Ozon) Phone number without country code: "); return Console.ReadLine()!;
            case "code": Console.Write("(Ozon) Authentication Code: "); return Console.ReadLine()!;
            case "email_code": Console.Write("(Ozon) Email authentication code: "); return Console.ReadLine()!;
            default: return null!;
        }
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
        FullMode = BoundedChannelFullMode.DropOldest
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

using (var host = builder.Build())
{
    var client = host.Services.GetRequiredService<WTelegram.Client>();
    var updateRouter = host.Services.GetRequiredService<UpdateHandler>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var scrappers = host.Services.GetServices<IScrapper>();

    _ = await client.LoginUserIfNeeded();
    _ = client.WithUpdateManager(updateRouter.Handle);
    
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