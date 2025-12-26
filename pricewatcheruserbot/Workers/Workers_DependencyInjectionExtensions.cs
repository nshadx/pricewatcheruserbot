using System.Threading.Channels;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot.Workers;

public static class Workers_DependencyInjectionExtensions
{
    public static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        var channel = Channel.CreateBounded<WorkerItem>(
            new BoundedChannelOptions(32)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        
        services.AddSingleton<ChannelReader<WorkerItem>>(channel);
        services.AddSingleton<ChannelWriter<WorkerItem>>(resolver =>
        {
            var hostLifetime = resolver.GetRequiredService<IHostApplicationLifetime>();
    
            hostLifetime.ApplicationStopping.Register(() => { channel.Writer.Complete(); });
    
            return channel;
        });

        services.AddHostedService<ProducerWorker>();
        services.AddHostedService<ConsumerWorker>();

        return services;
    }
}