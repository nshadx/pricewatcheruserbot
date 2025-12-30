using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot;
using pricewatcheruserbot.Browser;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Telegram;
using pricewatcheruserbot.UserInput;
using pricewatcheruserbot.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(x =>
{
    x.UseSqlite(builder.Configuration.GetConnectionString("DbConnection"));
});

builder.AddUserInput();
builder.AddBrowserServices();
builder.AddTelegram();
builder.AddMessageServices();
builder.AddScrappers();
builder.AddCommandHandlers();
builder.AddWorkers();

using (var host = builder.Build())
{
    await using (var scope = host.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
    
    var telegramService = host.Services.GetRequiredService<TelegramService>();
    var scrapperService = host.Services.GetRequiredService<ScrapperService>();
    
    await telegramService.Authorize();
    await scrapperService.Authorize();

    await host.RunAsync();
}