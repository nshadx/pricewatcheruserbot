using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using pricewatcheruserbot;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Configuration;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using TL;
using Channel = System.Threading.Channels.Channel;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotCredentials>(builder.Configuration.GetRequiredSection(nameof(BotCredentials)));
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton(provider =>
{
    var botCredentials = provider.GetRequiredService<IOptions<BotCredentials>>().Value;
    var logger = provider.GetRequiredService<ILogger<WTelegram.Client>>();

    var file = File.Open(Environment.GetEnvironmentVariable("TG_Session_FilePath") ?? throw new ArgumentException("you should provide session file path"), FileMode.OpenOrCreate);

    Func<string, string?> config = what =>
    {
        switch (what)
        {
            case "api_id": return botCredentials.ApiId;
            case "api_hash": return botCredentials.ApiHash;
            case "password": return botCredentials.Password;
            case "phone_number": return botCredentials.PhoneNumber;
            case "verification_code": Console.Write("Verification code: "); return Console.ReadLine();
            default: return null;
        }
    };
    
    WTelegram.Helpers.Log = (logLevel, message) =>
    {
        logger.Log((LogLevel)logLevel, message);
    };
    
    var client = new WTelegram.Client(
        config,
        file
    );
    
    return client;
});
builder.Services.AddKeyedSingleton("ozon", (Func<string, string>)(what =>
    {
        switch (what)
        {
            case "phone_number": Console.Write("Phone number without country code: "); return Console.ReadLine()!;
            case "code": Console.Write("Code: "); return Console.ReadLine()!;
            case "email_code": Console.Write("Email code: "); return Console.ReadLine()!;
            default: return null!;
        }
    })
);

builder.Services.AddDbContext<AppDbContext>(x =>
{
    var connectionString = builder.Configuration.GetConnectionString("DbConnection");
    
    x.UseSqlite(connectionString);
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
builder.Services.AddSingleton(
    Channel.CreateBounded<WorkerItem>(
        capacity: 1024
    )
);
builder.Services.AddHostedService<ProducerWorker>();
builder.Services.AddHostedService<ConsumerWorker>();

using (var host = builder.Build())
{
    var client = host.Services.GetRequiredService<WTelegram.Client>();
    var updateRouter = host.Services.GetRequiredService<UpdateHandler>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    _ = await client.LoginUserIfNeeded();
    _ = client.WithUpdateManager(updateRouter.Handle);

    await using (var scope = host.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scrappers = scope.ServiceProvider.GetServices<IScrapper>();

        await dbContext.Database.EnsureCreatedAsync();
        
        try
        { 
            foreach (var scrapper in scrappers)
            {
                await scrapper.Authorize();
            }

            logger.LogInformation("Success authorization in services");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authorization in services");
            await host.StopAsync();
            return;
        }
    }

    await host.RunAsync();
}