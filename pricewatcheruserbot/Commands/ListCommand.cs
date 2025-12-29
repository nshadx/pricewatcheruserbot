using pricewatcheruserbot.Telegram;
using TL;

namespace pricewatcheruserbot.Commands;

public record ListCommand(Message Message)
{
    public static ListCommand Parse(Message message)
    {
        return new(message);
    }
    
    public class Handler(
        MessageManager messageManager
    )
    {
        public async Task Handle(ListCommand command)
        {
            await messageManager.SetCurrentNewList();
            await messageManager.DeleteMessage(command.Message);
        }
    }
}