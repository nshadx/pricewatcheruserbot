namespace pricewatcheruserbot.Telegram;

public class TelegramService(
    ILogger<TelegramService> logger,
    UpdateHandler updateHandler,
    WTelegram.Client client,
    TelegramInput input
)
{
    public async Task Authorize()
    {
        logger.LogInformation("Telegram login started...");

        var phoneNumber = await input.GetPhoneNumber();
        await DoLogin(phoneNumber);

        logger.LogInformation("Telegram login completed successfully");
        logger.LogInformation("Telegram receiver started");
    }
    
    private async Task DoLogin(string loginInfo)
    {
        while (client.User is null)
        {
            loginInfo = await client.Login(loginInfo) switch
            {
                "password" => await input.GetPassword(),
                "verification_code" => await input.GetPhoneVerificationCode(),
                _ => null!
            };
        }

        updateHandler.UserId = client.UserId;
    }
}