using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesScrapper(
    ILogger<WildberriesScrapper> logger,
    BrowserService browserService,
    IOptions<BrowserConfiguration> configuration
) : ScrapperBase(logger, browserService, configuration)
{
    public override Uri Host { get; } = new("https://wildberries.ru");
    
    protected override Task AuthorizeCore()
    {
        return Task.CompletedTask;
    }

    protected override async Task<double> GetPriceCore()
    {
        PageObject pageObject = new(Page);
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

        Logger.LogInformation("Price was received successfully"); 
        await TakeScreenshot("wildberries_price_received");
        
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