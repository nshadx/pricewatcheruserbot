using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Browser.Impl;

public class WebUserAgentFetcher(HttpClient httpClient) : IUserAgentFetcher
{
    public async IAsyncEnumerable<string> Enumerate()
    {
        var response = await httpClient.GetStreamAsync(EnvironmentVariables.UserAgentFetchUrl);

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