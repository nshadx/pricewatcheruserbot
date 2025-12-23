using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public class ScrapperFactory(
    IEnumerable<IScrapper> scrappers
)
{
    public IScrapper GetScrapper(Uri url)
    {
        var scrapperList = scrappers.ToList();
        
        return url.Host switch
        {
            "www.ozon.ru" or "ozon.ru" => scrapperList.OfType<OzonScrapper>().Single(),
            "www.wildberries.ru" or "wildberries.ru" => scrapperList.OfType<WildberriesScrapper>().Single(),
            "www.market.yandex.ru" or "market.yandex.ru" => scrapperList.OfType<YandexMarketScrapper>().Single(),
            var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
        };
    }
}