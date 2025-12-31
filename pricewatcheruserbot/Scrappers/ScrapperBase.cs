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
        
        await Page.GotoAsync(BaseUrl);
        
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

    public async Task<bool> IsAuthorized()
    {
        Page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        await Page.GotoAsync(BaseUrl);
        
        Logger.LogInformation("Page loaded");
        await TakeScreenshot("authorization_page_loaded");

        try
        {
            return await IsAuthorizedCore();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    public async Task<double> GetPrice(string url)
    {
        if (IsUrlBelongsHost(url))
        {
            throw new InvalidOperationException("Unsupported url");
        }
        
        Page = await BrowserService.CreateNewPageWithinContext();
        
        Logger.LogInformation("Page init...");
        
        await Page.GotoAsync(url);
        
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
    
    public abstract string BaseUrl { get; }
    
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
    protected abstract Task<bool> IsAuthorizedCore();
    protected abstract Task<double> GetPriceCore();
    
    private bool IsUrlBelongsHost(string url) => new Uri(url).Host.Replace("www", string.Empty) == new Uri(BaseUrl).Host.Replace("www", string.Empty);
}