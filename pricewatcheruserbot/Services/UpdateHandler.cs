using pricewatcheruserbot.Commands;
using TL;

namespace pricewatcheruserbot.Services;

public class UpdateHandler(
    IServiceProvider serviceProvider,
    WTelegram.Client client
)
{
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
            await client.SendMessageAsync(
                peer: new InputPeerSelf(),
                text: ex.Message
            );
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
            var messageId = message.id;
            
            if (text.StartsWith("/add"))
            {
                var instance = AddCommand.Parse(text, messageId);
                await addCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/rem"))
            {
                var instance = RemoveCommand.Parse(text, messageId);
                await removeCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/lst"))
            {
                var instance = ListCommand.Parse(text, messageId);
                await listCommandHandler.Handle(instance);
            }
            else
            {
                throw new ArgumentException($"Unknown command: {text}");
            }
        }
    }

    private bool SentToSavedMessages(MessageBase message) => message.Peer is PeerUser pu && pu.user_id == client.UserId;
}