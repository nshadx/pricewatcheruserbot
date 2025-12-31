using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Entities;

namespace pricewatcheruserbot.Workers;

public class WorkerItemTracker(IMemoryCache memoryCache)
{
    private const int _windowSize = 5;
    
    public bool IsPriceDecreased(WorkerItem workerItem, double currentPrice, out double difference)
    {
        difference = 0;
        var isPriceDecreased = false;
        
        if (memoryCache.TryGetValue<SimpleMovingAverage>(workerItem.Id, out var sma))
        {
            if (sma is not null && sma.TryGetLatestValue(out var value))
            {
                isPriceDecreased = currentPrice < value;
                difference = sma.Previous - currentPrice;
            }
        }
        
        sma ??= new(5);
        sma.Update((int)currentPrice);
        
        memoryCache.Set(workerItem.Id, sma);
        
        return isPriceDecreased;
    }

    public void Remove(WorkerItem workerItem)
    {
        memoryCache.Remove(workerItem.Id);
    }
}