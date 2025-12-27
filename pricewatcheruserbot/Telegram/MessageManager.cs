using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Services;
using TL;

namespace pricewatcheruserbot.Telegram;

public class MessageManager(
    WTelegram.Client client,
    MessageSender messageSender,
    AppDbContext dbContext
)
{
    public async Task UpdateAllLists()
    {
        var workerItems = await dbContext.WorkerItems
            .OrderBy(x => x.Order)
            .ToListAsync();
        var listMessages = dbContext.SentMessages
            .Where(x => x.Type == MessageType.LIST)
            .AsAsyncEnumerable();
        
        await foreach (var message in listMessages)
        {
            await messageSender.Edit_WorkerItemList(workerItems, message);
        }
    }

    public async Task SetCurrentNewList()
    {
        var workerItems = await dbContext.WorkerItems
            .OrderBy(x => x.Order)
            .ToListAsync();
        var newMessage = await messageSender.Send_WorkerItemList(workerItems);
        
        var listMessages = dbContext.SentMessages
            .Where(x => x.Type == MessageType.LIST)
            .AsAsyncEnumerable();
        
        await foreach (var message in listMessages)
        {
            await client.DeleteMessages(
                peer: InputPeer.Self,
                id: message.TelegramId
            );
            dbContext.SentMessages.Remove(message);
        }

        SentMessage sentMessage = new()
        {
            TelegramId = newMessage.id,
            Type = MessageType.LIST
        };

        await dbContext.SentMessages.AddAsync(sentMessage);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteMessage(Message message)
    {
        await client.DeleteMessages(
            peer: InputPeer.Self,
            id: message.id
        );
    }
}