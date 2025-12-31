using System.Threading.Channels;
using pricewatcheruserbot.Services;

namespace pricewatcheruserbot.Workers;

public static class Workers_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddWorkers()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<WorkerItemTracker>();
            
            var channel = Channel.CreateBounded<WorkerItem>(
                new BoundedChannelOptions(32)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
        
            builder.Services.AddSingleton<ChannelReader<WorkerItem>>(channel);
            builder.Services.AddSingleton<ChannelWriter<WorkerItem>>(resolver =>
            {
                var hostLifetime = resolver.GetRequiredService<IHostApplicationLifetime>();
    
                hostLifetime.ApplicationStopping.Register(() => { channel.Writer.Complete(); });
    
                return channel;
            });

            builder.Services.AddHostedService<ProducerWorker>();
            builder.Services.AddHostedService<ConsumerWorker>();

            return builder;
        }
    }
}