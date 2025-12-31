using pricewatcheruserbot.Services;
using pricewatcheruserbot.Telegram;
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
        WorkerItemService workerItemService,
        MessageManager messageManager
    )
    {
        public async Task Handle(RemoveCommand command)
        {
            var id = workerItemService.Remove(command.Order);

            await messageManager.UpdateAllLists();
            await messageManager.DeleteMessage(command.Message);
        }
    }
}