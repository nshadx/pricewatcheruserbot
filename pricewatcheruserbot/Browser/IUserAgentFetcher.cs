namespace pricewatcheruserbot.Browser;

public interface IUserAgentFetcher
{
    IAsyncEnumerable<string> Enumerate();
}