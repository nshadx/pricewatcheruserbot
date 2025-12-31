using System.Threading.Channels;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Utils;

namespace pricewatcheruserbot.Workers;

public class ProducerWorker(
    WorkerItemService workerItemService,
    ChannelWriter<WorkerItem> globalChannel
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var enumerable = workerItemService.GetAll();
            foreach (var workerItem in enumerable)
            {
                await globalChannel.WriteAsync(workerItem, stoppingToken);
            }

            await DelayUtils.ProducerDelay(stoppingToken);
        }
    }
}