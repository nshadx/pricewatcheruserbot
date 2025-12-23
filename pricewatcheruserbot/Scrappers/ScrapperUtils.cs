using Microsoft.Playwright;

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

    public static Task Debug_TakeScreenshot(this IPage page, string name) => page.ScreenshotAsync(new PageScreenshotOptions() { Path = $"/data/screenshots/{name}"});
}