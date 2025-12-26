using Microsoft.Playwright;
using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Scrappers;

public static class ScrapperUtils
{
    public static double GetPriceValueWithoutCurrency(string? price)
    {
        if (string.IsNullOrEmpty(price))
        {
            return double.MaxValue;
        }
        
        var onlyDigits = price
            .Where(char.IsDigit)
            .ToArray();
        
        var value = double.Parse(onlyDigits);

        return value;
    }

    public static Task Debug_TakeScreenshot(this IPage page, string name)
    {
        var path = Path.Combine(EnvironmentVariables.StorageDirectoryPath, "screenshots", $"{name}.png");
        
        return page.ScreenshotAsync(new PageScreenshotOptions() { Path = path });
    }
}