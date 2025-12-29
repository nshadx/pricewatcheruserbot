namespace pricewatcheruserbot.Entities;

public class SentMessage
{
    public int Id { get; set; }
    public int TelegramId { get; set; }
    public MessageType Type { get; set; }
}