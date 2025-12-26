using Microsoft.Playwright;

namespace pricewatcheruserbot.Browser.Patchers;

public class WebDriverPatcher : IPatcher
{
    public Task BeforeLaunch(BrowserTypeLaunchOptions options)
    {
        var args = options.Args?.ToList() ?? [];

        AddOrReplaceArgument(args, "--disable-blink-features", "AutomationControlled", true);
        AddOrReplaceArgument(args, "--no-sandbox", null, false);
        AddOrReplaceArgument(args, "--start-maximized", null, false);
        AddOrReplaceArgument(args, "--disable-dev-shm-usage", null, false);
        
        var idx = args.FindIndex(x => x.StartsWith("--disable-blink-features="));
        if (idx != -1)
        {
            var arg = args[idx];
            args[idx] = $"{arg}, AutomationControlled";
        }
        else
        {
            args.Add("--disable-blink-features=AutomationControlled");
        }
        
        options.Args = args.ToArray();
        
        return Task.CompletedTask;
    }

    public Task OnNewContextCreated(BrowserNewContextOptions options)
    {
        options.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        options.ViewportSize = new ViewportSize() { Height = 1080, Width = 1920 };
        options.Locale = "ru-RU";

        return Task.CompletedTask;
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