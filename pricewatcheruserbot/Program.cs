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
    
    var client = new WTelegram.Client(
        Config,
        File.Open(Environment.GetEnvironmentVariable("TG_Session_FilePath") ?? throw new ArgumentException("you should provide session file path"), FileMode.OpenOrCreate)
    );
    WTelegram.Helpers.Log = (logLevel, message) =>
    {
        logger.Log((LogLevel)logLevel, message);
    };
    
    string? Config(string what)
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
    }
    
    return client;
});

builder.Services.AddDbContext<AppDbContext>(x =>
{
    var connectionString = builder.Configuration.GetConnectionString("DbConnection");
    
    x.UseSqlite(connectionString);
});
builder.Services.AddMemoryCache();

builder.Services.AddScoped<AddCommand.Handler>();
builder.Services.AddScoped<ListCommand.Handler>();
builder.Services.AddScoped<RemoveCommand.Handler>();

builder.Services.AddSingleton<OzonScrapper>();
builder.Services.AddSingleton<WildberriesScrapper>();
builder.Services.AddSingleton<YandexMarketScrapper>();

builder.Services.AddSingleton<BrowserService>();
builder.Services.AddSingleton<ScrapperFactory>();
builder.Services.AddSingleton(
    Channel.CreateBounded<WorkerItem>(
        capacity: 1024
    )
);
builder.Services.AddHostedService<ProducerWorker>();
builder.Services.AddHostedService<ConsumerWorker>();

var host = builder.Build();

var client = host.Services.GetRequiredService<WTelegram.Client>();
var updateRouter = host.Services.GetRequiredService<UpdateHandler>();

_ = await client.LoginUserIfNeeded();
_ = client.WithUpdateManager(updateRouter.Handle);

await using (var scope = host.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    await dbContext.Database.EnsureCreatedAsync();
}

await host.RunAsync();