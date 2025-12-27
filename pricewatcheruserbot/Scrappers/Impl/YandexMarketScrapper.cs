using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class YandexMarketScrapper(
    ILogger<YandexMarketScrapper> logger,
    BrowserService browserService
) : ScrapperBase(logger, browserService)
{
    public override Uri Host { get; } = new("https://market.yandex.ru");
    
    protected override Task AuthorizeCore(IPage page)
    {
        return Task.CompletedTask;
    }

    protected override async Task<double> GetPriceCore(IPage page)
    {
        PageObject pageObject = new(page);

        Logger.LogInformation("Trying to close login box...");
            
        await pageObject.CloseLoginBox();
            
        Logger.LogInformation("Login box closed");
        await page.Debug_TakeScreenshot("yandex_market_login_box_closed");
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);
        
        Logger.LogInformation("Price was received successfully"); 
        await page.Debug_TakeScreenshot("yandex_market_price_received");

        return priceValue;
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