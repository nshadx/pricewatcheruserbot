namespace pricewatcheruserbot.Scrappers;

public class ScrapperService(
    ScrapperServiceInput input,
    ILogger<ScrapperService> logger,
    IEnumerable<ScrapperBase> scrappers
)
{
    public async Task Authorize()
    {
        foreach (var scrapper in scrappers)
        {
            if (await scrapper.IsAuthorized() || !await input.HasAccount(scrapper.BaseUrl))
            {
                continue;
            }
            
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