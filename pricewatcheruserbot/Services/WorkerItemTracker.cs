using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot.Services;

public class WorkerItemTracker(IMemoryCache memoryCache)
{
    public bool IsPriceDecreased(WorkerItem workerItem, double currentPrice, out double difference)
    {
        difference = 0;
        
        if (memoryCache.TryGetValue<double>(workerItem.Id, out var previousPrice))
        {
            difference = previousPrice - currentPrice;
        }

        memoryCache.Set(workerItem.Id, currentPrice);
        
        return difference > 0;
    }

    public void Remove(WorkerItem workerItem)
    {
        memoryCache.Remove(workerItem.Id);
    }
}