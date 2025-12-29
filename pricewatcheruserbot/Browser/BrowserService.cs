using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace pricewatcheruserbot.Browser;

public class BrowserService : IAsyncDisposable, IDisposable
{
    private IPlaywright? _instance;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;

    private readonly IReadOnlyCollection<IPatcher> _patchers;
    private readonly BrowserConfiguration _configuration;
    
    public BrowserService(
        IEnumerable<IPatcher> patchers,
        IOptions<BrowserConfiguration> configuration
    )
    {
        _patchers = patchers.ToArray();
        _configuration = configuration.Value;
    }

    public async Task SaveState()
    {
        await Initialize();

        await _browserContext.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = _configuration.SessionFilePath });
    }
    
    public async Task<IPage> CreateNewPageWithinContext()
    {
        await Initialize();

        var page = await _browserContext.NewPageAsync();

        foreach (var patcher in _patchers)
        {
            await patcher.OnPageCreated(page);
        }

        return page;
    }
    
    [MemberNotNull(nameof(_instance))]
    [MemberNotNull(nameof(_browser))]
    [MemberNotNull(nameof(_browserContext))]
    private async Task Initialize()
    {
        _instance ??= await Playwright.CreateAsync();

        if (_browser is null)
        {
            BrowserTypeLaunchOptions options = new();
            options.Headless = true;
            options.Channel = "chrome";
            
            foreach (var patcher in _patchers)
            {
                await patcher.BeforeLaunch(options);
            }

            _browser = await _instance.Chromium.LaunchAsync(options);
        }

        if (_browserContext is null)
        {
            BrowserNewContextOptions options = new();

            if (File.Exists(_configuration.SessionFilePath))
                options.StorageStatePath = _configuration.SessionFilePath;

            foreach (var patcher in _patchers)
            {
                await patcher.OnNewContextCreated(options);
            }
            
            _browserContext = await _browser.NewContextAsync(options);
        }
    }
    
    public void Dispose()
    {
        if (_browserContext is IDisposable browserContextDisposable)
        {
            browserContextDisposable.Dispose();
        }
        
        if (_browser is IDisposable browserDisposable)
        {
            browserDisposable.Dispose();
        }
        
        _instance?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browserContext != null)
        {
            await _browserContext.DisposeAsync();
        }
        
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        
        if (_instance is IAsyncDisposable instanceAsyncDisposable)
        {
            await instanceAsyncDisposable.DisposeAsync();
        }
        else
        {
            _instance?.Dispose();
        }
    }
}