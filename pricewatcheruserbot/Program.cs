using pricewatcheruserbot.Browser;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Configuration;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Telegram;
using pricewatcheruserbot.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAppDbContext();
builder.Services.AddTelegram();
builder.Services.AddBrowserServices();
builder.Services.AddScrappers();
builder.Services.AddCommandHandlers();
builder.Services.AddWorkers();
builder.Services.AddUserInputProviders();
builder.Services.AddTrackerServices();
builder.Services.AddMessageServices();

using (var host = builder.Build())
{
    var telegramService = host.Services.GetRequiredService<TelegramService>();
    var scrapperService = host.Services.GetRequiredService<ScrapperService>();
    
    await telegramService.Authorize();
    await scrapperService.Authorize();
    
    await using (var scope = host.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }

    await host.RunAsync();
}