using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using TL;
using Channel = System.Threading.Channels.Channel;

#if DEBUG
DotNetEnv.Env.Load();
DotNetEnv.Env.TraversePath();
#endif

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton(provider =>
{
    var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();

    var session = File.Open(EnvironmentVariables.TelegramSessionFilePath, FileMode.OpenOrCreate);

    Func<string, string?> config = what =>
    {
#if DEBUG
        switch (what)
        {
            case "api_id": return EnvironmentVariables.ApiId;
            case "api_hash": return EnvironmentVariables.ApiHash;
            case "password": return EnvironmentVariables.Password;
            case "phone_number": return EnvironmentVariables.PhoneNumber;
            case "verification_code": Console.WriteLine("(Telegram) Enter verification code from Telegram Application: "); return Console.ReadLine();
            default: return null;
        } 
#else
        switch (what)
        {
            case "api_id": Console.Write("(Telegram) Enter api_id: "); return Console.ReadLine();
            case "api_hash": Console.Write("(Telegram) Enter api_hash: "); return Console.ReadLine();
            case "password": Console.Write("(Telegram) Enter password: "); return Console.ReadLine();
            case "phone_number": Console.Write("(Telegram) Enter phone number with country code (+7): "); return Console.ReadLine();
            case "verification_code": Console.Write("(Telegram) Enter verification code from Telegram Application: "); return Console.ReadLine();
            default: return null;
        }
#endif
    };

    WTelegram.Helpers.Log = (logLevel, message) =>
    {
#pragma warning disable CA2254
        logger.Log((LogLevel)(logLevel - 1), message);
#pragma warning restore CA2254
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
            case "phone_number": Console.WriteLine("(Ozon) Phone number without country code: "); return Console.ReadLine()!;
            case "code": Console.WriteLine("(Ozon) Authentication Code: "); return Console.ReadLine()!;
            case "email_code": Console.WriteLine("(Ozon) Email authentication code: "); return Console.ReadLine()!;
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