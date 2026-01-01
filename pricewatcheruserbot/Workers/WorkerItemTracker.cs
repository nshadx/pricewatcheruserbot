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
                var minimalDifference = GetPercentOf(value, 0.3);
                difference = sma.Previous - currentPrice;
                isPriceDecreased = difference > minimalDifference;
            }
        }
        else
        {
            sma = new(_windowSize);
        }
        
        sma.Update(currentPrice);

        workerItemService.UpdateSma(id, sma);
        
        return isPriceDecreased;
    }
    
    private double GetPercentOf(double value, double percent) => value / 100 * percent;
}