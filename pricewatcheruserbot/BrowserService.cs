using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright;

namespace pricewatcheruserbot;

public class BrowserService : IAsyncDisposable
{
    private IBrowserContext? _browserContext;

    public async Task<IBrowserContext> GetBrowserContext()
    {
        await Initialize();

        return _browserContext;
    }
    
    [MemberNotNull(nameof(_browserContext))]
    private async Task Initialize()
    {
        if (_browserContext is null)
        {
            var instance = await Playwright.CreateAsync();
            var browser = await instance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false, Args = ["--disable-blink-features=AutomationControlled", "--start-minimized"] });

            _browserContext = await browser.NewContextAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browserContext != null)
        {
            await _browserContext.DisposeAsync();
        }
    }
}