using System.Threading.Channels;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot;

public class ProducerWorker(
    IServiceProvider serviceProvider
) : BackgroundService
{
    private static readonly TimeSpan _delay = TimeSpan.FromMinutes(4);
    
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
            
            await Task.Delay(_delay, stoppingToken);
        }
    }
}