using System.Threading.Channels;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using pricewatcheruserbot.Scrappers.Impl;
using pricewatcheruserbot.Services;

namespace pricewatcheruserbot.Workers;

public class ConsumerWorker(
    ILogger<ConsumerWorker> logger,
    ChannelReader<WorkerItem> globalChannel,
    ScrapperProvider scrapperProvider,
    WorkerItemTracker workerItemTracker,
    MessageSender messageSender
) : BackgroundService
{
    private readonly Channel<(IScrapper Scrapper, WorkerItem WorkerItem)> _ozonChannel = Channel.CreateBounded<(IScrapper Scrapper, WorkerItem WorkerItem)>(1);
    private readonly Channel<(IScrapper Scrapper, WorkerItem WorkerItem)> _yandexChannel = Channel.CreateBounded<(IScrapper Scrapper, WorkerItem WorkerItem)>(1);
    private readonly Channel<(IScrapper Scrapper, WorkerItem WorkerItem)> _wildberriesChannel = Channel.CreateBounded<(IScrapper Scrapper, WorkerItem WorkerItem)>(1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enumerable = globalChannel.ReadAllAsync(stoppingToken);
        
        var workerTask = Task.WhenAll(
            HandleWorkerItem(_ozonChannel, stoppingToken),
            HandleWorkerItem(_yandexChannel, stoppingToken),
            HandleWorkerItem(_wildberriesChannel, stoppingToken)
        );

        Dictionary<Type, Channel<(IScrapper Scrapper, WorkerItem WorkerItem)>> router = new()
        {
            [typeof(OzonScrapper)] = _ozonChannel,
            [typeof(WildberriesScrapper)] = _wildberriesChannel,
            [typeof(YandexMarketScrapper)] = _yandexChannel
        };
        
        var parallelTask = Parallel.ForEachAsync(
            source: enumerable,
            body: async (workerItem, stoppingToken) =>
            {
                var scrapper = scrapperProvider.GetByUrl(workerItem.Url);
                var channel = router[scrapper.GetType()];

                await channel.Writer.WriteAsync((scrapper, workerItem), stoppingToken);
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

    private async Task HandleWorkerItem(ChannelReader<(IScrapper Scrapper, WorkerItem WorkerItem)> channel, CancellationToken stoppingToken)
    {
        await foreach (var (scrapper, workerItem) in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Start handling {workerItem}...", workerItem);
                
                var price = await scrapper.GetPrice(workerItem.Url);

                logger.LogInformation("Price received for {workerItem}", workerItem);

                if (workerItemTracker.IsPriceDecreased(workerItem, price, out var difference))
                {
                    await messageSender.Send_PriceDropped(workerItem, difference);
                    logger.LogInformation("Price dropped by {difference} for {workerItem}", difference, workerItem);
                }
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