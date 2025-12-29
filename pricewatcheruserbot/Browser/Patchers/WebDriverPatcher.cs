using Microsoft.Playwright;

namespace pricewatcheruserbot.Browser.Patchers;

public class WebDriverPatcher(
    IServiceProvider serviceProvider
) : IPatcher
{
    public Task BeforeLaunch(BrowserTypeLaunchOptions options)
    {
        var args = options.Args?.ToList() ?? [];

        AddOrReplaceArgument(args, "--disable-blink-features", "AutomationControlled", true);
        AddOrReplaceArgument(args, "--no-sandbox", null, false);
        AddOrReplaceArgument(args, "--start-maximized", null, false);
        AddOrReplaceArgument(args, "--disable-dev-shm-usage", null, false);
        
        options.Args = args.ToArray();
        
        return Task.CompletedTask;
    }

    public async Task OnNewContextCreated(BrowserNewContextOptions options)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var userAgentProvider = scope.ServiceProvider.GetRequiredService<UserAgentProvider>();
            
            options.UserAgent = await userAgentProvider.GetRandomUserAgent("Chrome");
        }
        options.ViewportSize = new ViewportSize() { Height = 1080, Width = 1920 };
        options.Locale = "ru-RU";
    }

    public async Task OnPageCreated(IPage page)
    {
        await page.AddInitScriptAsync("""
                                      delete Object.getPrototypeOf(navigator).webdriver;
                                      window.navigator.chrome = { runtime: {} };
                                      """);
    }

    private void AddOrReplaceArgument(List<string> args, string name, string? value, bool append)
    {
        var idx = args.FindIndex(x => x.StartsWith(name));
        if (idx is not -1)
        {
            var arg = args[idx];

            if (append)
            {
                args[idx] = $"{arg}, {value}";
            }
            else if (value is not null)
            {
                args[idx] = value;
            }
        }
        else if (value is null)
        {
            args.Add(name);
        }
        else
        {
            args.Add($"{name}={value}");
        }
    }
}