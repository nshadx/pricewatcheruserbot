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
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(10);
   
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var channel = scope.ServiceProvider.GetRequiredService<ChannelReader<WorkerItem>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConsumerWorker>>();
            
            var enumerable = channel.ReadAllAsync(stoppingToken);
            
            await Parallel.ForEachAsync(
                source: enumerable.Take(Environment.ProcessorCount),
                parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount },
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
            var scrapperFactory = scope.ServiceProvider.GetRequiredService<ScrapperFactory>();
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var client = scope.ServiceProvider.GetRequiredService<WTelegram.Client>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConsumerWorker>>();
            
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

        for (int i = 0; i < count; i++)
        {
            int index = Random.Shared.Next(emojis.Length);
            result += emojis[index];
        }

        return result;
    }
}