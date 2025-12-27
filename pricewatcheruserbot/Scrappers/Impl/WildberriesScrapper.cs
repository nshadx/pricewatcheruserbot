using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesScrapper(
    ILogger<WildberriesScrapper> logger,
    BrowserService browserService
) : ScrapperBase(logger, browserService)
{
    public override Uri Host { get; } = new("https://wildberries.ru");
    
    protected override Task AuthorizeCore(IPage page)
    {
        return Task.CompletedTask;
    }

    protected override async Task<double> GetPriceCore(IPage page)
    {
        PageObject pageObject = new(page);
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

        Logger.LogInformation("Price was received successfully"); 
        await page.Debug_TakeScreenshot("wildberries_price_received");
            
        await BrowserService.SaveState();
            
        return priceValue;
    }

    private class PageObject(IPage page)
    {
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@class, 'priceBlockPrice')]").First;

            var result = await locator.TextContentAsync();

            return result;
        }
    }
}