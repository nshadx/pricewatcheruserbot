using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using pricewatcheruserbot;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Configuration;
using pricewatcheruserbot.Scrappers;
using TL;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotCredentials>(builder.Configuration.GetRequiredSection(nameof(BotCredentials)));
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton(provider =>
{
    var botCredentials = provider.GetRequiredService<IOptions<BotCredentials>>().Value;
    
    var client = new WTelegram.Client(Config);
    
    string? Config(string what)
    {
        switch (what)
        {
            case "api_id": return botCredentials.ApiId;
            case "api_hash": return botCredentials.ApiHash;
            case "password": return botCredentials.Password;
            case "phone_number": return botCredentials.PhoneNumber;
            case "verification_code": Console.Write("Verification code: "); return Console.ReadLine();
            case "first_name": throw new NotImplementedException();
            case "last_name": throw new NotImplementedException();
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

builder.Services.AddSingleton<BrowserService>();
builder.Services.AddSingleton<OzonScrapper>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

var client = host.Services.GetRequiredService<WTelegram.Client>();
var updateRouter = host.Services.GetRequiredService<UpdateHandler>();

_ = await client.LoginUserIfNeeded();
_ = client.WithUpdateManager(updateRouter.Handle);

await using (var scope = host.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

await host.RunAsync();