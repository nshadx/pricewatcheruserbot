using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesScrapper(
    ILogger<WildberriesScrapper> logger,
    BrowserService browserService
) : IScrapper
{
    public Task Authorize()
    {
        return Task.CompletedTask;
    }

    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        logger.LogInformation("Page init...");
        
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync(url.ToString());
        
        logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("wildberries_price_page_loaded");
        
        try
        {
            PageObject pageObject = new(page);
            
            logger.LogInformation("Begin price selecting...");
            
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

            logger.LogInformation("Price was received successfully"); 
            await page.Debug_TakeScreenshot("wildberries_price_received");
            
            return priceValue;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private class PageObject(IPage page)
    {
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@class, 'priceBlockPrice')]/descendant::h2").First;

            var result = await locator.TextContentAsync();

            return result;
        }
    }
}