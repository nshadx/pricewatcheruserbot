using pricewatcheruserbot.Browser;
using pricewatcheruserbot.Commands;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Telegram;
using pricewatcheruserbot.UserInput;
using pricewatcheruserbot.Workers;

Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.AddDb();
builder.AddServices();
builder.AddUserInput();
builder.AddBrowserServices();
builder.AddTelegram();
builder.AddMessageServices();
builder.AddScrappers();
builder.AddCommandHandlers();
builder.AddWorkers();

using (var host = builder.Build())
{
    var dbService = host.Services.GetRequiredService<DbService>();
    var telegramService = host.Services.GetRequiredService<TelegramService>();
    var scrapperService = host.Services.GetRequiredService<ScrapperService>();
    
    await dbService.Migrate();
    await telegramService.Authorize();
    await scrapperService.Authorize();

    await host.RunAsync();
}