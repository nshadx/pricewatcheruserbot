using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using TL;

namespace pricewatcheruserbot;

public class ConsumerWorker(
    IServiceProvider serviceProvider
) : BackgroundService
{
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var channel = scope.ServiceProvider.GetRequiredService<Channel<WorkerItem>>();

                await _semaphore.WaitAsync(stoppingToken);
                var workerItem = await channel.Reader.ReadAsync(stoppingToken);
                _ = CallHandler(workerItem);

                async Task CallHandler(WorkerItem workerItem)
                {
                    await Task.Yield();
                    
                    try
                    {
                        await HandleWorkerItem(workerItem);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
        }
    }

    private async Task HandleWorkerItem(WorkerItem workerItem)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var scrapperFactory = scope.ServiceProvider.GetRequiredService<ScrapperFactory>();
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var client = scope.ServiceProvider.GetRequiredService<WTelegram.Client>();
            
            var scrapper = scrapperFactory.GetScrapper(workerItem.Url);
            var price = await scrapper.GetPrice(workerItem.Url);

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
}