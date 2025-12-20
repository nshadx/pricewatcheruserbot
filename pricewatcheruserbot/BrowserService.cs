using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright;

namespace pricewatcheruserbot;

public class BrowserService
{
    private IBrowser? _browser;

    public async Task<IBrowser> GetBrowser()
    {
        await Initialize();

        return _browser;
    }
    
    [MemberNotNull(nameof(_browser))]
    private async Task Initialize()
    {
        if (_browser is null)
        {
            var instance = await Playwright.CreateAsync();
            _browser = await instance.Chromium.LaunchAsync();
        }
    }
}