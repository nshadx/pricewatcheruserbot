namespace pricewatcheruserbot.Scrappers;

public interface IScrapper
{
    Task<double> GetPrice(Uri url);
}