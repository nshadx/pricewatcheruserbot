using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright;
using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Browser;

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
            var browser = await instance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
            {
                Headless = true,
                Channel = "chrome",
                Args =
                [
                    "--no-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    "--start-maximized",
                    "--disable-dev-shm-usage"
                ]
            });
            
            string? storageStatePath = null;
            if (File.Exists(EnvironmentVariables.BrowserSessionFilePath))
            {
                storageStatePath = EnvironmentVariables.BrowserSessionFilePath;
            }
            
            _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions()
            {
                StorageStatePath = storageStatePath,
                ViewportSize = new ViewportSize() { Height = 1080, Width = 1920 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                Locale = "ru-RU"
            });
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