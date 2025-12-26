using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public static class Scrappers_DependencyInjectionExtensions
{
    public static IServiceCollection AddScrappers(this IServiceCollection services)
    {
        services.AddSingleton<ScrapperProvider>();
        services.AddSingleton<IScrapper, OzonScrapper>();
        services.AddSingleton<IScrapper, WildberriesScrapper>();
        services.AddSingleton<IScrapper, YandexMarketScrapper>();

        return services;
    }
}