using pricewatcheruserbot.Commands;
using TL;

namespace pricewatcheruserbot;

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

    private Task HandleMessage(MessageBase message)
    {
        if (!SentToSavedMessages(message) || message is not Message { message: string text })
        {
            return Task.CompletedTask;
        }

        if (text.StartsWith('/'))
        {
            return HandleCommand(text);
        }

        return Task.CompletedTask;
    }

    private async Task HandleCommand(string text)
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var addCommandHandler = scope.ServiceProvider.GetRequiredService<AddCommand.Handler>();
            var listCommandHandler = scope.ServiceProvider.GetRequiredService<ListCommand.Handler>();
            var removeCommandHandler = scope.ServiceProvider.GetRequiredService<RemoveCommand.Handler>();
            
            if (text.StartsWith("/add"))
            {
                var instance = AddCommand.Parse(text);
                await addCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/rem"))
            {
                var instance = RemoveCommand.Parse(text);
                await removeCommandHandler.Handle(instance);
            }
            else if (text.StartsWith("/lst"))
            {
                var instance = ListCommand.Parse(text);
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