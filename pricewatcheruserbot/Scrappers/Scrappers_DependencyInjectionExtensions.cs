using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public static class Scrappers_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddScrappers()
        {
            builder.Services.AddSingleton<OzonInput>();
            builder.Services.AddSingleton<WildberriesInput>();
            builder.Services.AddSingleton<YandexInput>();
            builder.Services.AddSingleton<ScrapperServiceInput>();
            builder.Services.AddSingleton<ScrapperProvider>();
            builder.Services.AddSingleton<ScrapperService>();
            builder.Services.AddSingleton<ScrapperBase, OzonScrapper>();
            builder.Services.AddSingleton<ScrapperBase, WildberriesScrapper>();
            builder.Services.AddSingleton<ScrapperBase, YandexMarketScrapper>();

            return builder;
        }
    }
}