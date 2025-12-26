using pricewatcheruserbot.Scrappers.Impl;

namespace pricewatcheruserbot.Scrappers;

public class ScrapperProvider
{
    private readonly IScrapper _ozonScrapper;
    private readonly IScrapper _wildberriesScrapper;
    private readonly IScrapper _yandexMarketScrapper;
    
    public ScrapperProvider(IEnumerable<IScrapper> scrappers)
    {
        var array = scrappers.ToArray();

        _ozonScrapper = array
            .OfType<OzonScrapper>()
            .Single();
        _wildberriesScrapper = array
            .OfType<WildberriesScrapper>()
            .Single();
        _yandexMarketScrapper = array
            .OfType<YandexMarketScrapper>()
            .Single();
    }

    public IScrapper GetByUrl(Uri url)
    {
        return url.Host switch
        {
            "www.ozon.ru" or "ozon.ru" => _ozonScrapper,
            "www.wildberries.ru" or "wildberries.ru" => _wildberriesScrapper,
            "www.market.yandex.ru" or "market.yandex.ru" => _yandexMarketScrapper,
            var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
        };
    }
}