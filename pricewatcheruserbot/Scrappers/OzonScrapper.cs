using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers;

public class OzonScrapper(BrowserService browserService) : IScrapper
{
    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowser();
        var page = await browser.NewPageAsync();
        
        await page.GotoAsync(url.ToString());

        PageObject pageObject = new(page);
        var priceString = await pageObject.GetPrice();
        var priceValue = GetPriceValueWithoutCurrency(priceString);

        await page.CloseAsync();
        
        return priceValue;
    }

    private static double GetPriceValueWithoutCurrency(string price)
    {
        var onlyDigits = price
            .Where(char.IsDigit)
            .ToArray();
        
        var value = double.Parse(onlyDigits);

        return value;
    }

    private class PageObject(IPage page)
    {
        public async Task<string> GetPrice()
        {
            var locator = page
                .Locator("//div[contains(@data-widget, 'webPrice')]/descendant::span").First
                .Or(
                    page.Locator("//div/child::span[text()='c Ozon Картой']/../div/span")
                ).First;

            var result = await locator.TextContentAsync() ?? throw new InvalidOperationException();

            return result;
        }
    }
}