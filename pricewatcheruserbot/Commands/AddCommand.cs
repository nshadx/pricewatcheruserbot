using pricewatcheruserbot.Services;
using pricewatcheruserbot.Telegram;
using TL;

namespace pricewatcheruserbot.Commands;

public record AddCommand(Message Message, string Url)
{
    public static AddCommand Parse(Message message)
    {
        var args = message.message["/add".Length..];
        var urlString = args.Trim();

        if (!Uri.TryCreate(urlString, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Unknown url format: {urlString}");
        }

        return new(message, urlString);
    }

    public class Handler(
        WorkerItemService workerItemService,
        MessageManager messageManager
    )
    {
        public async Task Handle(AddCommand command)
        {
            workerItemService.Add(command.Url);
            await messageManager.UpdateAllLists();
            await messageManager.DeleteMessage(command.Message);
        }
    }
}