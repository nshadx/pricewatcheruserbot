using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using TL;

namespace pricewatcheruserbot.Commands;

public class ListCommand
{
    public static ListCommand Parse(string command)
    {
        return new();
    }
    
    public class Handler(
        AppDbContext dbContext,
        WTelegram.Client client
    )
    {
        public async Task Handle(ListCommand command)
        {
            var workerItems = await dbContext.WorkerItems
                .OrderBy(x => x.Order)
                .ToListAsync();
            var lines = workerItems.Select(x => x.ToString());
            var text = workerItems.Count == 0 ? "<empty>" : string.Join('\n', lines);

            var message = await client.SendMessageAsync(
                peer: new InputPeerSelf(),
                text: text
            );
            SentMessage sentMessage = new()
            {
                TelegramId = message.id,
                Type = MessageType.LIST,
            };
            
            await dbContext.SentMessages.AddAsync(sentMessage);
            await dbContext.SaveChangesAsync();
        }
    }
}