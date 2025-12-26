using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using TL;
using Channel = System.Threading.Channels.Channel;

namespace pricewatcheruserbot.Workers;

public class ConsumerWorker(
    ILogger<ConsumerWorker> logger,
    IEnumerable<IScrapper> scrappers,
    IMemoryCache memoryCache,
    WTelegram.Client client,
    ChannelReader<WorkerItem> globalChannel
) : BackgroundService
{
    private readonly Channel<WorkerItem> _ozonChannel = Channel.CreateBounded<WorkerItem>(1);
    private readonly Channel<WorkerItem> _yandexChannel = Channel.CreateBounded<WorkerItem>(1);
    private readonly Channel<WorkerItem> _wildberriesChannel = Channel.CreateBounded<WorkerItem>(1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enumerable = globalChannel.ReadAllAsync(stoppingToken);
        
        var workerTask = Task.WhenAll(
            HandleWorkerItem(_ozonChannel, stoppingToken),
            HandleWorkerItem(_yandexChannel, stoppingToken),
            HandleWorkerItem(_wildberriesChannel, stoppingToken)
        );
        var parallelTask = Parallel.ForEachAsync(
            source: enumerable,
            body: async (workerItem, stoppingToken) =>
            {
                switch (workerItem.Url.Host)
                {
                    case "www.ozon.ru" or "ozon.ru":
                        await _ozonChannel.Writer.WriteAsync(workerItem, stoppingToken);
                    break;
                    case "www.market.yandex.ru" or "market.yandex.ru":
                        await _yandexChannel.Writer.WriteAsync(workerItem, stoppingToken);
                    break;
                    case "www.wildberries.ru" or "wildberries.ru":
                        await _wildberriesChannel.Writer.WriteAsync(workerItem, stoppingToken);
                    break;
                }
            },
            cancellationToken: stoppingToken
        );
        var task = Task.WhenAll(workerTask, parallelTask);

        await task;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _ozonChannel.Writer.Complete();
        _yandexChannel.Writer.Complete();
        _wildberriesChannel.Writer.Complete();
        
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleWorkerItem(ChannelReader<WorkerItem> channel, CancellationToken stoppingToken)
    {
        await foreach (var workerItem in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Start handling {workerItem}...", workerItem);

                IScrapper scrapper = workerItem.Url.Host switch
                {
                    "www.ozon.ru" or "ozon.ru" => scrappers.OfType<OzonScrapper>().Single(),
                    "www.wildberries.ru" or "wildberries.ru" => scrappers.OfType<WildberriesScrapper>().Single(),
                    "www.market.yandex.ru" or "market.yandex.ru" => scrappers.OfType<YandexMarketScrapper>().Single(),
                    var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
                };

                var price = await scrapper.GetPrice(workerItem.Url);

                logger.LogInformation("Price received for {workerItem}", workerItem);

                if (memoryCache.TryGetValue<double>(workerItem.Id, out var previousPrice))
                {
                    var difference = previousPrice - price;

                    if (difference > 0)
                    {
                        var text = $"{MessageUtils.GenerateRandomEmojis(3)}: The item's ({workerItem}) price has dropped by {difference}";

                        await client.SendMessageAsync(
                            peer: new InputPeerSelf(),
                            text: text
                        );

                        logger.LogInformation("Price dropped by {difference} for {workerItem}", difference, workerItem);
                    }
                }
                
                memoryCache.Set(workerItem.Id, price);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Item handling completed with error");
                continue;
            }
            finally
            {
                await DelayUtils.RandomNext(cancellationToken: stoppingToken);
            }
        }
    }
}