using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Telegram;
using pricewatcheruserbot.Workers;
using TL;

namespace pricewatcheruserbot.Commands;

public record RemoveCommand(Message Message, int Order)
{
    public static RemoveCommand Parse(Message message)
    {
        var args = message.message["/rem".Length..];
        var orderString = args.Trim();

        if (!int.TryParse(orderString, out var order))
        {
            throw new ArgumentException($"Unknown order: {orderString}");
        }

        return new(message, order);
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
            await messageManager.DeleteMessage(command.Message);
        }
    }
}