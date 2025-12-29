namespace pricewatcheruserbot.Telegram;

public class TelegramConfiguration
{
    public int ApiId { get; set; }
    public string ApiHash { get; set; } = null!;
    public string SessionFilePath { get; set; } = null!;
}