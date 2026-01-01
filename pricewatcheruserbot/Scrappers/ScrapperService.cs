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
            try
            {
                if (await scrapper.IsAuthorized() || !await input.HasAccount(scrapper.BaseUrl))
                {
                    logger.LogInformation("{baseUrl} is authorized or skipped", scrapper.BaseUrl);
                    
                    continue;
                }
                
                logger.LogInformation("Begin login for {baseUrl}...", scrapper.BaseUrl);

                await scrapper.Authorize();

                logger.LogInformation("Successful login");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed login for {baseUrl}", scrapper.BaseUrl);
            }
        }
    }
}