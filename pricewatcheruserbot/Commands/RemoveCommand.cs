using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pricewatcheruserbot.Services;
using TL;

namespace pricewatcheruserbot.Commands;

public class RemoveCommand
{
    public int MessageId { get; set; }
    public int Order { get; set; }
    
    public static RemoveCommand Parse(Message message)
    {
        var args = message.message["/rem".Length..];
        var orderString = args.Trim();

        if (!int.TryParse(orderString, out var order))
        {
            throw new ArgumentException($"Unknown order: {orderString}");
        }

        return new()
        {
            MessageId = message.id,
            Order = order
        };
    }
    
    public class Handler(
        AppDbContext dbContext,
        WorkerItemTracker workerItemTracker,
        MessageManager messageManager
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

            workerItemTracker.Remove(workerItem);

            await messageManager.UpdateAllLists();
            await messageManager.DeleteCommandMessage(command.MessageId);
        }
    }
}