using pricewatcheruserbot.Browser.Impl;
using pricewatcheruserbot.Browser.Patchers;

namespace pricewatcheruserbot.Browser;

public static class Browser_DependencyInjectionExtensions
{
    public static IServiceCollection AddBrowserServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<BrowserService>();
        services.AddSingleton<IPatcher, WebDriverPatcher>();
        services.AddScoped<UserAgentProvider>();
        services.AddSingleton<IUserAgentFetcher, WebUserAgentFetcher>();
        services.AddHostedService<UserAgentRefresher>();
        
        return services;
    }
}