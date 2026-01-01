using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace pricewatcheruserbot.Browser;

public class BrowserService : IAsyncDisposable, IDisposable
{
    private IPlaywright? _instance;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private int _roundCount;
    
    private readonly SemaphoreSlim _semaphore;
    private readonly IReadOnlyCollection<IPatcher> _patchers;
    private readonly string _localStorageFilePath;
    private readonly string _cookiesFilePath;
    
    public BrowserService(
        IEnumerable<IPatcher> patchers,
        IOptions<BrowserConfiguration> configuration
    )
    {
        _patchers = patchers.ToArray();
        _semaphore = new(1);

        Directory.CreateDirectory(configuration.Value.SessionDirectory);
        _localStorageFilePath = Path.Combine(configuration.Value.SessionDirectory, "local-storage.json");
        _cookiesFilePath = Path.Combine(configuration.Value.SessionDirectory, "cookies.json");
    }

    public async Task SaveState()
    {
        await Initialize();

        await _semaphore.WaitAsync();
        
        await _browserContext.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = _localStorageFilePath });
        
        var cookies = await _browserContext.CookiesAsync();
        var json = JsonSerializer.Serialize(cookies);
        await File.WriteAllTextAsync(_cookiesFilePath, json);

        _semaphore.Release();
    }
    
    public async Task<IPage> CreateNewPageWithinContext()
    {
        await Initialize();

        var page = await _browserContext.NewPageAsync();
        
        foreach (var patcher in _patchers)
        {
            await patcher.OnPageCreated(page);
        }

        Interlocked.Increment(ref _roundCount);
        
        return page;
    }
    
    [MemberNotNull(nameof(_instance))]
    [MemberNotNull(nameof(_browser))]
    [MemberNotNull(nameof(_browserContext))]
    private async Task Initialize()
    {
        await _semaphore.WaitAsync();
        
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

        if (_browserContext is null || _roundCount >= 3)
        {
            BrowserNewContextOptions options = new();

            if (File.Exists(_localStorageFilePath))
            {
                options.StorageStatePath = _localStorageFilePath;
            }

            foreach (var patcher in _patchers)
            {
                await patcher.OnNewContextCreated(options);
            }
            
            _browserContext = await _browser.NewContextAsync(options);
            _roundCount = 0;
            
            if (File.Exists(_cookiesFilePath))
            {
                using (var stream = File.OpenRead(_cookiesFilePath))
                {
                    var cookies = JsonSerializer.Deserialize<IEnumerable<Cookie>>(stream);
                    if (cookies is not null)
                    {
                        await _browserContext.AddCookiesAsync(cookies);
                    }
                }
            }
        }

        _semaphore.Release();
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