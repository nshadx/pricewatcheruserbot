using pricewatcheruserbot.Browser.Patchers;

namespace pricewatcheruserbot.Browser;

public static class Browser_DependencyInjectionExtensions
{
    public static IServiceCollection AddBrowserServices(this IServiceCollection services)
    {
        services.AddSingleton<BrowserService>();
        services.AddSingleton<IPatcher, WebDriverPatcher>();

        return services;
    }
}