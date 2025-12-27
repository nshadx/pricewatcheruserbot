using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers;

public abstract class ScrapperBase
{
    protected ScrapperBase(
        ILogger logger,
        BrowserService browserService
    )
    {
        Logger = logger;
        BrowserService = browserService;
    }

    public async Task Authorize()
    {
        var page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        await page.GotoAsync(Host.ToString(), new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle });
        
        Logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("authorization_page_loaded");

        try
        {
            await AuthorizeCore(page);
            await BrowserService.SaveState();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<double> GetPrice(Uri url)
    {
        if (IsUrlBelongsHost(url))
        {
            throw new InvalidOperationException("Unsupported url");
        }
        
        var page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        page.SetDefaultTimeout(15000);
        await page.GotoAsync(url.ToString());
        
        Logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("ozon_price_page_loaded");

        try
        {
            var result = await GetPriceCore(page);
            await BrowserService.SaveState();
            
            return result;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
    
    public abstract Uri Host { get; }
    protected BrowserService BrowserService { get; }
    protected ILogger Logger { get; }
    protected abstract Task AuthorizeCore(IPage page);
    protected abstract Task<double> GetPriceCore(IPage page);

    private bool IsUrlBelongsHost(Uri url) => url.Host.Replace("www", string.Empty) == Host.Host.Replace("www", string.Empty);
}