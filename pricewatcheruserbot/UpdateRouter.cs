using TL;

namespace pricewatcheruserbot;

public class UpdateRouter(WTelegram.Client client)
{
    public Task Handle(Update update)
    {
        return update switch
        {
            UpdateNewMessage unm => HandleMessage(unm.message),
            _ => Task.CompletedTask
        };
    }

    private async Task HandleMessage(MessageBase message)
    {
        if (!SentToSavedMessages(message))
        {
            return;
        }

        await Task.Delay(1);
    }

    private bool SentToSavedMessages(MessageBase message) => message.Peer is PeerUser pu && pu.user_id == client.UserId;
}