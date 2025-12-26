using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using TL;

namespace pricewatcheruserbot.Services;

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

    public async Task SendNewList()
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
                peer: new InputPeerSelf(),
                id: message.TelegramId
            );
            dbContext.SentMessages.Remove(message);
        }

        await dbContext.SentMessages.AddAsync(new SentMessage
        {
            TelegramId = newMessage.id,
            Type = MessageType.LIST
        });

        await dbContext.SaveChangesAsync();
    }

    public Task DeleteCommandMessage(int messageId) => client.DeleteMessages(
        peer: new InputPeerSelf(), 
        id: messageId
    );
}