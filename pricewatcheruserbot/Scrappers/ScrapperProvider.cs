using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public class ScrapperProvider
{
    private readonly ScrapperBase _ozonScrapperBase;
    private readonly ScrapperBase _wildberriesScrapperBase;
    private readonly ScrapperBase _yandexMarketScrapperBase;
    
    public ScrapperProvider(IEnumerable<ScrapperBase> scrappers)
    {
        var array = scrappers.ToArray();

        _ozonScrapperBase = array
            .OfType<OzonScrapper>()
            .Single();
        _wildberriesScrapperBase = array
            .OfType<WildberriesScrapper>()
            .Single();
        _yandexMarketScrapperBase = array
            .OfType<YandexMarketScrapper>()
            .Single();
    }

    public ScrapperBase GetByUrl(string url)
    {
        return new Uri(url).Host switch
        {
            "www.ozon.ru" or "ozon.ru" => _ozonScrapperBase,
            "www.wildberries.ru" or "wildberries.ru" => _wildberriesScrapperBase,
            "www.market.yandex.ru" or "market.yandex.ru" => _yandexMarketScrapperBase,
            var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
        };
    }
}