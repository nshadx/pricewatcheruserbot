using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using TL;

namespace pricewatcheruserbot.Commands;

public class RemoveCommand
{
    public int MessageId { get; set; }
    public int Order { get; set; }
    
    public static RemoveCommand Parse(string command, int messageId)
    {
        var args = command["/rem".Length..];
        var orderString = args.Trim();

        if (!int.TryParse(orderString, out var order))
        {
            throw new ArgumentException($"Unknown order: {orderString}");
        }

        return new()
        {
            MessageId = messageId,
            Order = order
        };
    }
    
    public class Handler(
        AppDbContext dbContext,
        WTelegram.Client client
    )
    {
        public async Task Handle(RemoveCommand command)
        {
            var workerItem = await dbContext.WorkerItems
                .Where(x => x.Order == command.Order)
                .SingleAsync();
            var itemsToUpdate = dbContext.WorkerItems
                .Where(x => x.Order > command.Order)
                .AsAsyncEnumerable();
            
            await foreach (var item in itemsToUpdate)
            {
                item.Order--;
            }

            dbContext.WorkerItems.Remove(workerItem);
            await dbContext.SaveChangesAsync();

            var listMessages = dbContext.SentMessages
                .Where(x => x.Type == MessageType.LIST)
                .AsAsyncEnumerable();
            var workerItems = await dbContext.WorkerItems
                .OrderBy(x => x.Order)
                .ToListAsync();
            var lines = workerItems.Select(x => x.ToString());
            var text = workerItems.Count == 0 ? "<empty>" : string.Join('\n', lines);
            
            await foreach (var message in listMessages)
            {
                await client.Messages_EditMessage(
                    peer: new InputPeerSelf(),
                    id: message.TelegramId,
                    message: text
                );
            }
            
            await client.DeleteMessages(
                peer: new InputPeerSelf(),
                command.MessageId
            );
        }
    }
}