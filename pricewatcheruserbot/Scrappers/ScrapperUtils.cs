namespace pricewatcheruserbot.Scrappers;

public static class ScrapperUtils
{
    public static double GetPriceValueWithoutCurrency(string price)
    {
        var onlyDigits = price
            .Where(char.IsDigit)
            .ToArray();
        
        var value = double.Parse(onlyDigits);

        return value;
    }
}