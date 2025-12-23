namespace pricewatcheruserbot.Scrappers;

public interface IScrapper
{
    Task Authorize();
    Task<double> GetPrice(Uri url);
}