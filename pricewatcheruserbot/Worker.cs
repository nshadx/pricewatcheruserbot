using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Scrappers;
using TL;

namespace pricewatcheruserbot;

public class Worker(
    IServiceProvider serviceProvider,
    WTelegram.Client client
) : BackgroundService
{
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(5);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

                var workerItems = dbContext.WorkerItems.AsAsyncEnumerable();

                await foreach (var workerItem in workerItems)
                {
                    var price = 0d;
                    
                    if (workerItem.Url.Host == "ozon.ru" || workerItem.Url.Host == "www.ozon.ru")
                    {
                        var ozonScrapper = scope.ServiceProvider.GetRequiredService<OzonScrapper>();
                        price = await ozonScrapper.GetPrice(workerItem.Url);
                    }

                    if (memoryCache.TryGetValue<double>(workerItem.Id, out var previousPrice))
                    {
                        if (price < previousPrice)
                        {
                            var difference = previousPrice - price;
                            var text = $"The item's ({workerItem}) price has dropped by {difference}";
                            
                            await client.SendMessageAsync(
                                peer: new InputPeerSelf(),
                                text: text
                            );
                        }
                    }

                    memoryCache.Set(workerItem.Id, price);
                }
            }
            
            await Task.Delay(_delay, stoppingToken);
        }
    }
}