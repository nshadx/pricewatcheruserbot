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
            var browser = await instance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });

            string? storageStatePath = null;
            if (File.Exists(Environment.GetEnvironmentVariable("Session_Storage")))
            {
                storageStatePath = Environment.GetEnvironmentVariable("Session_Storage");
            }
            
            _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions() { StorageStatePath = storageStatePath });
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