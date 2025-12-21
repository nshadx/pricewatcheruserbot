using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonScrapper(BrowserService browserService) : IScrapper
{
    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowser();
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
                .Locator("//div[contains(@data-widget, 'webPrice')]/descendant::span").First
                .Or(
                    page.Locator("//div/child::span[text()='c Ozon Картой']/../div/span")
                ).First;

            var result = await locator.TextContentAsync() ?? throw new InvalidOperationException();

            return result;
        }
    }
}