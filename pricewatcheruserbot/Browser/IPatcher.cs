using Microsoft.Playwright;

namespace pricewatcheruserbot.Browser;

public interface IPatcher
{
    Task BeforeLaunch(BrowserTypeLaunchOptions options);
    Task OnNewContextCreated(BrowserNewContextOptions options);
    Task OnPageCreated(IPage page);
}