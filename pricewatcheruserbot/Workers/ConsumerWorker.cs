using System.Threading.Channels;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Telegram;
using pricewatcheruserbot.Utils;

namespace pricewatcheruserbot.Workers;

public class ConsumerWorker(
    ILogger<ConsumerWorker> logger,
    ChannelReader<WorkerItem> globalChannel,
    ScrapperProvider scrapperProvider,
    WorkerItemTracker workerItemTracker,
    MessageSender messageSender
) : BackgroundService
{
    private readonly Channel<(ScrapperBase Scrapper, WorkerItem Item)> _ozonChannel = Channel.CreateBounded<(ScrapperBase Scrapper, WorkerItem Item)>(1);
    private readonly Channel<(ScrapperBase Scrapper, WorkerItem Item)> _yandexMarketChannel = Channel.CreateBounded<(ScrapperBase Scrapper, WorkerItem Item)>(1);
    private readonly Channel<(ScrapperBase Scrapper, WorkerItem Item)> _wildberriesChannel = Channel.CreateBounded<(ScrapperBase Scrapper, WorkerItem Item)>(1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enumerable = globalChannel.ReadAllAsync(stoppingToken);
        
        var workerTask = Task.WhenAll(
            HandleWorkerItem(_ozonChannel, stoppingToken),
            HandleWorkerItem(_yandexMarketChannel, stoppingToken),
            HandleWorkerItem(_wildberriesChannel, stoppingToken)
        );

        Dictionary<Type, Channel<(ScrapperBase Scrapper, WorkerItem Item)>> router = new()
        {
            [typeof(OzonScrapper)] = _ozonChannel,
            [typeof(WildberriesScrapper)] = _wildberriesChannel,
            [typeof(YandexMarketScrapper)] = _yandexMarketChannel
        };
        
        var parallelTask = Parallel.ForEachAsync(
            source: enumerable,
            body: async (item, stoppingToken) =>
            {
                var scrapper = scrapperProvider.GetByUrl(item.Url);
                var channel = router[scrapper.GetType()];

                await channel.Writer.WriteAsync((scrapper, item), stoppingToken);
            },
            cancellationToken: stoppingToken
        );
        var task = Task.WhenAll(workerTask, parallelTask);

        await task;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _ozonChannel.Writer.Complete();
        _yandexMarketChannel.Writer.Complete();
        _wildberriesChannel.Writer.Complete();
        
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleWorkerItem(ChannelReader<(ScrapperBase Scrapper, WorkerItem Item)> channel, CancellationToken stoppingToken)
    {
        await foreach (var (scrapper, item) in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Start handle {id}...", item.Id);
                
                var price = await scrapper.GetPrice(item.Url);

                logger.LogInformation("Price received for {id}", item.Id);

                if (workerItemTracker.IsPriceDecreased(item.Id, price, out var difference))
                {
                    await messageSender.Send_PriceDropped(item.Name, difference);
                    logger.LogInformation("Price dropped by {difference} for {id}", difference, item.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Item handle completed with error");
                continue;
            }
            finally
            {
                await DelayUtils.RandomNext(cancellationToken: stoppingToken);
            }
        }
    }
}