using Microsoft.Extensions.Options;

namespace pricewatcheruserbot.Browser.Impl;

public class WebUserAgentFetcher(
    HttpClient httpClient,
    IOptions<UserAgentConfiguration> configuration
) : IUserAgentFetcher
{
    public async IAsyncEnumerable<string> Enumerate()
    {
        foreach (var url in configuration.Value.FetchUrls)
        {
            var response = await httpClient.GetStreamAsync(url);

            using (var streamReader = new StreamReader(response))
            {
                while (streamReader.Peek() >= 0)
                {
                    var line = await streamReader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(line))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}