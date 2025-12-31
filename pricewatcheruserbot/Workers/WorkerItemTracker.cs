using pricewatcheruserbot.Services;

namespace pricewatcheruserbot.Workers;

public class WorkerItemTracker(
    WorkerItemService workerItemService
)
{
    private const int _windowSize = 5;
    
    public bool IsPriceDecreased(int id, double currentPrice, out double difference)
    {
        difference = 0;
        var isPriceDecreased = false;
        var sma = workerItemService.GetSma(id); 
        if (sma is not null)
        {
            if (sma.TryGetLatestValue(out var value))
            {
                isPriceDecreased = currentPrice < value;
                difference = sma.Previous - currentPrice;
            }
        }
        else
        {
            sma = new(_windowSize);
        }
        
        sma.Update((int)currentPrice);

        workerItemService.UpdateSma(id, sma);
        
        return isPriceDecreased;
    }
}