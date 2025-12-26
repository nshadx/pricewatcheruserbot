using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Services;
using TL;

namespace pricewatcheruserbot.Commands;

public class ListCommand
{
    public int MessageId { get; set; }
    
    public static ListCommand Parse(Message message)
    {
        return new() { MessageId = message.id };
    }
    
    public class Handler(
        AppDbContext dbContext,
        WTelegram.Client client
    )
    {
        public async Task Handle(ListCommand command)
        {
            var workerItems = await dbContext.WorkerItems
                .OrderBy(x => x.Order)
                .ToListAsync();
            var lines = workerItems.Select(x => x.ToString());
            var text = workerItems.Count == 0 ? "<empty>" : string.Join('\n', lines);

            var message = await client.SendMessageAsync(
                peer: new InputPeerSelf(),
                text: text
            );
            SentMessage sentMessage = new()
            {
                TelegramId = message.id,
                Type = MessageType.LIST,
            };
            var otherMessages = dbContext.SentMessages
                .Where(x => x.TelegramId != message.id)
                .AsAsyncEnumerable();
            await foreach (var otherMessage in otherMessages)
            {
                await client.DeleteMessages(
                    peer: new InputPeerSelf(),
                    otherMessage.TelegramId
                );
                dbContext.SentMessages.Remove(otherMessage);
            }
            
            await dbContext.SentMessages.AddAsync(sentMessage);
            await dbContext.SaveChangesAsync();
            
            await client.DeleteMessages(
                peer: new InputPeerSelf(),
                command.MessageId
            );
        }
    }
}