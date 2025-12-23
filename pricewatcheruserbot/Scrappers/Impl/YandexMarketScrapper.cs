using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class YandexMarketScrapper(BrowserService browserService) : IScrapper
{
    public Task Authorize()
    {
        return Task.CompletedTask;
    }

    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync(url.ToString());
        
        try
        {
            await page.GotoAsync(url.ToString());

            PageObject pageObject = new(page);

            await pageObject.CloseLoginBox();
            
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
        public async Task CloseLoginBox()
        {
            var locator = page
                .Locator("//div[contains(@data-baobab-name, 'loginPopup')]").First;

            try
            {
                var boundingBox = await locator.BoundingBoxAsync();

                if (boundingBox is not null)
                {
                    const int offset = 100;
                    var leftX = boundingBox.X - boundingBox.Width / 2 - offset;
                    var leftY = boundingBox.Y;
                
                    await page.Mouse.ClickAsync(leftX, leftY);
                }
            }
            catch { }
        }
        public async Task<string> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@data-auto, 'snippet-price-current')]/descendant::span[1]").First;

            var result = await locator.TextContentAsync() ?? throw new InvalidOperationException();

            return result;
        }
    }
}