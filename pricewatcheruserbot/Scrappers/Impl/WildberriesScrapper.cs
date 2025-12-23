using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesScrapper(BrowserService browserService) : IScrapper
{
    public Task Authorize()
    {
        return Task.CompletedTask;
    }

    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        try
        {
            await page.GotoAsync(url.ToString());

            PageObject pageObject = new(page);
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

            return priceValue;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private class PageObject(IPage page)
    {
        public async Task<string> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@class, 'priceBlockPrice')]/descendant::h2").First;

            var result = await locator.TextContentAsync() ?? throw new InvalidOperationException();

            return result;
        }
    }
}