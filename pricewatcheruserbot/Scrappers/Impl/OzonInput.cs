using pricewatcheruserbot.UserInput;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonInput(IUserInput input)
{
    public async Task<string> GetPhoneNumber() => await input.RequestAndWait("ozon phone number without country code", "ozon_phone_number") ?? throw new InvalidOperationException("Ozon phone number is REQUIRED");
    public async Task<string> GetPhoneVerificationCode() => await input.RequestAndWait("ozon phone verification code", "ozon_phone_verification_code") ?? throw new InvalidOperationException("Ozon phone verification code is REQUIRED");
    public async Task<string> GetEmailVerificationCode() => await input.RequestAndWait("ozon email verification code", "ozon_email_verification_code") ?? throw new InvalidOperationException("Ozon email verification code is REQUIRED");
}