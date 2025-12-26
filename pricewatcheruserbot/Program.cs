using pricewatcheruserbot.Browser;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Configuration;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAppDbContext();
builder.Services.AddTelegramClient();
builder.Services.AddBrowserServices();
builder.Services.AddScrappers();
builder.Services.AddCommandHandlers();
builder.Services.AddWorkers();
builder.Services.AddUserInputProviders();
builder.Services.AddTrackerServices();
builder.Services.AddMessageServices();

using (var host = builder.Build())
{
    var userInputProvider = host.Services.GetRequiredService<IUserInputProvider>();
    
    await userInputProvider.Init();
    
    var client = host.Services.GetRequiredService<WTelegram.Client>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var scrappers = host.Services.GetServices<IScrapper>();
    
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
    
    logger.LogInformation("Telegram receiver started");
    
    try
    { 
        foreach (var scrapper in scrappers)
        {
            await scrapper.Authorize();
        }

        logger.LogInformation("Successful authorization in web services");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed authorization in web services");
    }

    await using (var scope = host.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }

    await host.RunAsync();
}