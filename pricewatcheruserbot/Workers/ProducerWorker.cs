using System.Threading.Channels;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Services;

namespace pricewatcheruserbot.Workers;

public class ProducerWorker(
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = scope.ServiceProvider.GetRequiredService<ChannelWriter<WorkerItem>>();
                
                var enumerable = dbContext.WorkerItems.AsAsyncEnumerable();
                
                await foreach (var workerItem in enumerable)
                {
                    await channel.WriteAsync(workerItem, stoppingToken);
                }
            }

            await DelayUtils.ProducerDelay(stoppingToken);
        }
    }
}