using pricewatcheruserbot.UserInput;

namespace pricewatcheruserbot.Telegram;

public class TelegramInput(IUserInput userInput)
{
    public async Task<string> GetPhoneNumber() => await userInput.RequestAndWait("telegram phone number with country code (+7)", "telegram_phone_number") ?? throw new InvalidOperationException("Telegram phone number is REQUIRED");
    public async Task<string> GetPassword() => await userInput.RequestAndWait("telegram account password", "telegram_password") ?? throw new InvalidOperationException("Telegram account password is REQUIRED");
    public async Task<string> GetPhoneVerificationCode() => await userInput.RequestAndWait("telegram 2FA verification code from Telegram chat", "telegram_verification_code") ?? throw new InvalidOperationException("Telegram verification code is REQUIRED");
}