using pricewatcheruserbot.UserInput;

namespace pricewatcheruserbot.Scrappers;

public class ScrapperServiceInput(IUserInput input)
{
    public async Task<bool> HasAccount(string url) => await input.YesNoWait($"do you have any account for '{url}'");
}