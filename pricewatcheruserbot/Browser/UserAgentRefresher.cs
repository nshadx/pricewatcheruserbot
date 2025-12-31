using System.Threading.Channels;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Utils;

namespace pricewatcheruserbot.Browser;

public class UserAgentRefresher(
    IEnumerable<IUserAgentFetcher> fetchers,
    UserAgentService userAgentService
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var source = AsyncEnumerable.Empty<string>().Merge(fetchers.Select(x => x.Enumerate()));
            
            var channel = Channel.CreateUnbounded<string>();

            var producerTask = ProducerTask(source, channel.Writer, stoppingToken);
            var consumerTask = ConsumerTask(channel.Reader, stoppingToken);
            
            await Task.WhenAll(producerTask, consumerTask);
            await DelayUtils.UserAgentFetchDelay(stoppingToken);
        }
    }

    private async Task ProducerTask(IAsyncEnumerable<string> source, ChannelWriter<string> channel, CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(source, stoppingToken, channel.WriteAsync);
        channel.Complete();
    }

    private async Task ConsumerTask(ChannelReader<string> channel, CancellationToken stoppingToken)
    {
        await foreach (var value in channel.ReadAllAsync(stoppingToken))
        {
            userAgentService.Add(value);
        }
    }
}