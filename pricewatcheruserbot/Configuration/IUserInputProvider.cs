namespace pricewatcheruserbot.Configuration;

public interface IUserInputProvider
{
    Task<int> Telegram_GetApiId();
    Task<string> Telegram_GetApiHash();
    Task<string> Telegram_GetPassword();
    Task<string> Telegram_GetPhoneNumber();
    Task<string> Telegram_GetVerificationCode();

    Task<string> Ozon_GetPhoneNumber();
    Task<string> Ozon_GetEmailVerificationCode();
    Task<string> Ozon_GetPhoneVerificationCode();
}