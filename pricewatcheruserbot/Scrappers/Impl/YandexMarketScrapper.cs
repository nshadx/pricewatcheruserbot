using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class YandexMarketScrapper(
    ILogger<YandexMarketScrapper> logger,
    BrowserService browserService
) : IScrapper
{
    public Task Authorize()
    {
        return Task.CompletedTask;
    }

    public async Task<double> GetPrice(Uri url)
    {
        var page = await browserService.CreateNewPageWithinContext();
        
        logger.LogInformation("Page init...");
        
        await page.GotoAsync(url.ToString());
        
        logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("yandex_market_price_page_loaded");
        
        try
        {
            await page.GotoAsync(url.ToString());

            PageObject pageObject = new(page);

            logger.LogInformation("Trying to close login box...");
            
            await pageObject.CloseLoginBox();
            
            logger.LogInformation("Login box closed");
            await page.Debug_TakeScreenshot("yandex_market_login_box_closed");
            
            logger.LogInformation("Begin price selecting...");
            
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

            logger.LogInformation("Price was received successfully"); 
            await page.Debug_TakeScreenshot("yandex_market_price_received");

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

            if (await locator.IsVisibleAsync())
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
        }
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@data-auto, 'snippet-price-current')]/descendant::span[1]").First;

            var result = await locator.TextContentAsync();

            return result;
        }
    }
}