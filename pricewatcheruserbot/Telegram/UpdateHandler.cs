using pricewatcheruserbot.Commands;
using TL;

namespace pricewatcheruserbot.Telegram;

public class UpdateHandler(
    IServiceProvider serviceProvider,
    ILogger<UpdateHandler> logger
)
{
    public long UserId { get; set; }
    
    public async Task Handle(Update update)
    {
        var task = update switch
        {
            UpdateNewMessage unm => HandleMessage(unm.message),
            _ => Task.CompletedTask
        };

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during handle of update");
        }
    }

    private Task HandleMessage(MessageBase messageBase)
    {
        if (!SentToSavedMessages(messageBase) || messageBase is not Message message)
        {
            return Task.CompletedTask;
        }

        var text = message.message;
        
        if (text.StartsWith('/'))
        {
            return HandleCommand(message);
        }

        return Task.CompletedTask;
    }

    private async Task HandleCommand(Message message)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var addCommandHandler = scope.ServiceProvider.GetRequiredService<AddCommand.Handler>();
            var listCommandHandler = scope.ServiceProvider.GetRequiredService<ListCommand.Handler>();
            var removeCommandHandler = scope.ServiceProvider.GetRequiredService<RemoveCommand.Handler>();

            var text = message.message;

            if (text.StartsWith("/add"))
            {
                var instance = AddCommand.Parse(message);
                await addCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/rem"))
            {
                var instance = RemoveCommand.Parse(message);
                await removeCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/lst"))
            {
                var instance = ListCommand.Parse(message);
                await listCommandHandler.Handle(instance);
            }
            else
            {
                throw new ArgumentException($"Unknown command: {text}");
            }
        }
    }

    private bool SentToSavedMessages(MessageBase message) => message.Peer is PeerUser pu && pu.user_id == UserId;
}