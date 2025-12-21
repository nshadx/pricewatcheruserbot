using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public class ScrapperFactory(
    OzonScrapper ozonScrapper,
    WildberriesScrapper wildberriesScrapper,
    YandexMarketScrapper yandexMarketScrapper
)
{
    public IScrapper GetScrapper(Uri url)
    {
        return url.Host switch
        {
            "www.ozon.ru" => ozonScrapper,
            "ozon.ru" => ozonScrapper,
            "www.wildberries.ru" => wildberriesScrapper,
            "wildberries.ru" => wildberriesScrapper,
            "www.market.yandex.ru" => yandexMarketScrapper,
            "market.yandex.ru" => yandexMarketScrapper,
            var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
        };
    }
}