using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public static class Scrappers_DependencyInjectionExtensions
{
    public static IServiceCollection AddScrappers(this IServiceCollection services)
    {
        services.AddSingleton<ScrapperProvider>();
        services.AddSingleton<ScrapperService>();
        services.AddSingleton<ScrapperBase, OzonScrapper>();
        services.AddSingleton<ScrapperBase, WildberriesScrapper>();
        services.AddSingleton<ScrapperBase, YandexMarketScrapper>();

        return services;
    }
}