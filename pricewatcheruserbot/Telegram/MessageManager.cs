using pricewatcheruserbot.Services;
using TL;

namespace pricewatcheruserbot.Telegram;

public class MessageManager(
    WTelegram.Client client,
    MessageSender messageSender,
    WorkerItemService workerItemService,
    SentMessageService sentMessageService
)
{
    public async Task UpdateAllLists()
    {
        var names = workerItemService.GetNames();
        var lists = sentMessageService.DeleteAll(SentMessageType.List);
        foreach (var messageId in lists)
        {
            await messageSender.Edit_WorkerItemList(names, messageId);
        }
    }

    public async Task SetCurrentNewList()
    {
        var lists = sentMessageService.DeleteAll(SentMessageType.List);
        foreach (var id in lists)
        {
            await client.DeleteMessages(
                peer: InputPeer.Self,
                id: id
            );
        }

        var urls = workerItemService.GetNames();
        var newMessage = await messageSender.Send_WorkerItemList(urls);
        sentMessageService.Add(newMessage.id, SentMessageType.List);
    }

    public async Task DeleteMessage(Message message)
    {
        await client.DeleteMessages(
            peer: InputPeer.Self,
            id: message.id
        );
    }
}