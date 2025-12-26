using pricewatcheruserbot.Entities;
using TL;

namespace pricewatcheruserbot.Services;

public class MessageSender(
    WTelegram.Client client
)
{
    public async Task<Message> Send_WorkerItemList(IReadOnlyCollection<WorkerItem> workerItems)
    {
        var message = await client.SendMessageAsync(
            peer: new InputPeerSelf(),
            text: GetText_WorkerItemList(workerItems)
        );

        return message;
    }
    
    public async Task Edit_WorkerItemList(IReadOnlyCollection<WorkerItem> workerItems, SentMessage message)
    {
        _ = await client.Messages_EditMessage(
            peer: new InputPeerSelf(),
            id: message.TelegramId,
            message: GetText_WorkerItemList(workerItems)
        );
    }

    private string GetText_WorkerItemList(IReadOnlyCollection<WorkerItem> workerItems) => workerItems.Count == 0 ? "<empty>" : string.Join('\n', workerItems.Select(x => x.ToString()));
}