using pricewatcheruserbot.UserInput;

namespace pricewatcheruserbot.Scrappers.Impl;

public class YandexInput(IUserInput input)
{
    public async Task<string> GetAccount(IEnumerable<string> suggestedAccounts) => await input.RequestAndWait($"one of suggested account in list {string.Join(", ", suggestedAccounts.Select(x => $"'{x}'"))}", "yandex_suggested_account") ?? throw new InvalidOperationException("Yandex suggested account is REQUIRED");
    public async Task<string> GetPhoneNumber() => await input.RequestAndWait("yandex phone number with country code", "yandex_phone_number") ?? throw new InvalidOperationException("Yandex phone number is REQUIRED");
    public async Task<string> GetPhoneVerificationCode() => await input.RequestAndWait("yandex phone verification code", "yandex_phone_verification_code") ?? throw new InvalidOperationException("Yandex phone verification code is REQUIRED");
    public async Task<string> GetEmailVerificationCode() => await input.RequestAndWait("yandex email verification code", "yandex_email_verification_code") ?? throw new InvalidOperationException("Yandex email verification code is REQUIRED");
}