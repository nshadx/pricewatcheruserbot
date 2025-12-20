namespace pricewatcheruserbot.Scrappers;

public class ScrapperFactory(
    OzonScrapper ozonScrapper
)
{
    public IScrapper GetScrapper(Uri url)
    {
        return url.Host switch
        {
            "www.ozon.ru" => ozonScrapper,
            "ozon.ru" => ozonScrapper,
            var host => throw new InvalidOperationException($"Cannot resolve scrapper for host '{host}'")
        };
    }
}