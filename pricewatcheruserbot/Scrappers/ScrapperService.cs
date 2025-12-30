namespace pricewatcheruserbot.Scrappers;

public class ScrapperService(
    ILogger<ScrapperService> logger,
    IEnumerable<ScrapperBase> scrappers
)
{
    public async Task Authorize()
    {
        foreach (var scrapper in scrappers)
        {
            try
            {
                logger.LogInformation("Begin login for {host}...", scrapper.BaseUrl);

                await scrapper.Authorize();

                logger.LogInformation("Successful login");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed login for {host}", scrapper.BaseUrl);
            }
        }
    }
}