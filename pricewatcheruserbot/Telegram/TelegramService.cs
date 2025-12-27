using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Telegram;

public class TelegramService(
    ILogger<TelegramService> logger,
    IUserInputProvider userInputProvider,
    UpdateHandler updateHandler,
    WTelegram.Client client
)
{
    public async Task Authorize()
    {
        logger.LogInformation("Telegram login started...");
        
        var telegramPhoneNumber = await userInputProvider.Telegram_GetPhoneNumber();
        await DoLogin(telegramPhoneNumber);

        logger.LogInformation("Telegram login completed successfully");
        logger.LogInformation("Telegram receiver started");
    }
    
    private async Task DoLogin(string loginInfo)
    {
        while (client.User is null)
        {
            loginInfo = await client.Login(loginInfo) switch
            {
                "password" => await userInputProvider.Telegram_GetPassword(),
                "verification_code" => await userInputProvider.Telegram_GetVerificationCode(),
                _ => null!
            };
        }

        updateHandler.UserId = client.UserId;
    }
}