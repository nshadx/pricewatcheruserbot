using pricewatcheruserbot.UserInput;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesInput(IUserInput input)
{
    public async Task<string> GetPhoneNumber() => await input.RequestAndWait("wildberries phone number without country code", "wildberries_phone_number") ?? throw new InvalidOperationException("Wildberries phone number is REQUIRED");
    public async Task<string> GetPhoneVerificationCode() => await input.RequestAndWait("wildberries phone verification code", "wildberries_phone_verification_code") ?? throw new InvalidOperationException("Wildberries phone verification code is REQUIRED");
}