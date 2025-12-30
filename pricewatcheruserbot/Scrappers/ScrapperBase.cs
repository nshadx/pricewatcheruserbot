using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers;

public abstract class ScrapperBase
{
    protected ScrapperBase(
        ILogger logger,
        BrowserService browserService,
        IOptions<BrowserConfiguration> configuration
    )
    {
        Logger = logger;
        BrowserService = browserService;
        Configuration = configuration.Value;
    }

    public async Task Authorize()
    {
        Page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        await Page.GotoAsync(BaseUrl.ToString());
        
        Logger.LogInformation("Page loaded");
        await TakeScreenshot("authorization_page_loaded");

        try
        {
            await AuthorizeCore();
            await TakeScreenshot("authorization_successful");
            await BrowserService.SaveState();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    public async Task<double> GetPrice(Uri url)
    {
        if (IsUrlBelongsHost(url))
        {
            throw new InvalidOperationException("Unsupported url");
        }
        
        Page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        Page.SetDefaultTimeout(15000);
        await Page.GotoAsync(url.ToString());
        
        Logger.LogInformation("Page loaded");
        await TakeScreenshot("price_page_loaded");

        try
        {
            var result = await GetPriceCore();
            await BrowserService.SaveState();
            
            return result;
        }
        finally
        {
            await Page.CloseAsync();
        }
    }
    
    public abstract Uri BaseUrl { get; }
    
    protected BrowserService BrowserService { get; }
    protected ILogger Logger { get; }
    protected BrowserConfiguration Configuration { get; }
    protected IPage Page = null!;
    
    protected Task TakeScreenshot(string name)
    {
        var path = Path.Combine(Configuration.ScreenshotsDirectory, $"{name}.png");
        return Page.ScreenshotAsync(new PageScreenshotOptions() { Path = path });
    }
    
    protected abstract Task AuthorizeCore();
    protected abstract Task<double> GetPriceCore();
    
    private bool IsUrlBelongsHost(Uri url) => url.Host.Replace("www", string.Empty) == BaseUrl.Host.Replace("www", string.Empty);
}