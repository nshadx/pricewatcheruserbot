using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers;

public class OzonScrapper(BrowserService browserService)
{
    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowser();
        var page = await browser.NewPageAsync();
        
        await page.GotoAsync(url.ToString());

        PageObject pageObject = new(page);
        
        return 0;
    }

    private class PageObject(IPage page)
    {
    }
}