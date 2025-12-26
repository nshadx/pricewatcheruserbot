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
        MessageManager messageManager
    )
    {
        public async Task Handle(ListCommand command)
        {
            await messageManager.SetCurrentNewList();
            await messageManager.DeleteCommandMessage(command.MessageId);
        }
    }
}