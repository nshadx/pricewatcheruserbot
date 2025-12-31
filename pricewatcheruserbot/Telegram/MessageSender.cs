using TL;

namespace pricewatcheruserbot.Telegram;

public class MessageSender(
    WTelegram.Client client
)
{
    public async Task<Message> Send_PriceDropped(string name, double difference)
    {
        var message = await client.SendMessageAsync(
            peer: InputPeer.Self,
            text: $"{MessageUtils.GenerateRandomEmojis(3)}: The item's ({name}) price has dropped by {difference}"
        );

        return message;
    }
    
    public async Task<Message> Send_WorkerItemList(IEnumerable<string> names)
    {
        var message = await client.SendMessageAsync(
            peer: InputPeer.Self,
            text: GetText_WorkerItemList(names)
        );

        return message;
    }
    
    public async Task Edit_WorkerItemList(IEnumerable<string> names, int messageId)
    {
        _ = await client.Messages_EditMessage(
            peer: InputPeer.Self,
            id: messageId,
            message: GetText_WorkerItemList(names)
        );
    }

    private string GetText_WorkerItemList(IEnumerable<string> names) => string.Join('\n', names) is string s && !string.IsNullOrEmpty(s) ? s : "<empty>";
}