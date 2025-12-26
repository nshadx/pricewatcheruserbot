using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Scrappers;
using TL;

namespace pricewatcheruserbot.Workers;

public class ConsumerWorker(
    IServiceProvider serviceProvider,
    ChannelReader<WorkerItem> channel,
    ILogger<ConsumerWorker> logger,
    ScrapperFactory scrapperFactory,
    IMemoryCache memoryCache,
    WTelegram.Client client
) : BackgroundService
{
    private static readonly TimeSpan _delay = TimeSpan.FromMinutes(5);
    private const int _workers = 4;
   
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var enumerable = channel.ReadAllAsync(stoppingToken);
            
            await Parallel.ForEachAsync(
                source: enumerable.Take(_workers),
                parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = _workers },
                body: async (workerItem, _) =>
                {
                    try
                    {
                        logger.LogInformation("start: {workerItem}", workerItem);
                        await HandleWorkerItem(workerItem);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "an error occured during handling of url");
                    }
                }
            );

            await Task.Delay(_delay, stoppingToken);
        }
    }

    private async Task HandleWorkerItem(WorkerItem workerItem)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var scrapper = scrapperFactory.GetScrapper(workerItem.Url);
            var price = await scrapper.GetPrice(workerItem.Url);

            logger.LogInformation("price received for {workerItem}", workerItem);
            
            if (memoryCache.TryGetValue<double>(workerItem.Id, out var previousPrice))
            {
                var difference = previousPrice - price;
                
                if (difference > 0)
                {
                    var text = $"{GenerateRandomEmojis(3)}: The item's ({workerItem}) price has dropped by {difference}";
                            
                    await client.SendMessageAsync(
                        peer: new InputPeerSelf(),
                        text: text
                    );

                    logger.LogInformation("price dropped by {difference} for {workerItem}", difference, workerItem);
                }
            }

            logger.LogInformation("price updated for {workerItem}", workerItem);
            memoryCache.Set(workerItem.Id, price);
        }
    }
    
    private static string GenerateRandomEmojis(int count)
    {
        string[] emojis =
        [
            "🔥", "🎉", "💥", "✨", "🌟", "🚀", "❤️", "😎", "🤩", "🌈",
            "💫", "🎊", "💎", "🎵", "🕺", "🍕", "🍿", "⚡", "🥳", "👑"
        ];

        string result = "";

        for (var i = 0; i < count; i++)
        {
            int index = Random.Shared.Next(emojis.Length);
            result += emojis[index];
        }

        return result;
    }
}