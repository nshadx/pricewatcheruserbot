using pricewatcheruserbot.Browser.Impl;
using pricewatcheruserbot.Browser.Patchers;

namespace pricewatcheruserbot.Browser;

public static class Browser_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddBrowserServices()
        {
            builder.Services.Configure<BrowserConfiguration>(builder.Configuration.GetRequiredSection(nameof(BrowserConfiguration)));
            builder.Services.Configure<UserAgentConfiguration>(builder.Configuration.GetRequiredSection(nameof(UserAgentConfiguration)));
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<BrowserService>();
            builder.Services.AddSingleton<IPatcher, WebDriverPatcher>();
            builder.Services.AddScoped<UserAgentProvider>();
            builder.Services.AddSingleton<IUserAgentFetcher, WebUserAgentFetcher>();
            builder.Services.AddHostedService<UserAgentRefresher>();

            return builder;
        }
    }
}