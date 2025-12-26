using Microsoft.EntityFrameworkCore;
using pricewatcheruserbot.Entities;
using pricewatcheruserbot.Services;
using TL;

namespace pricewatcheruserbot.Commands;

public class AddCommand
{
    public int MessageId { get; set; }
    public Uri Url { get; set; } = null!;
    
    public static AddCommand Parse(Message message)
    {
        var args = message.message["/add".Length..];
        var urlString = args.Trim();

        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url))
        {
            throw new ArgumentException($"Unknown url format: {urlString}");
        }
        
        return new()
        {
            MessageId = message.id,
            Url = url
        };
    }

    public class Handler(
        AppDbContext dbContext,
        WTelegram.Client client
    )
    {
        public async Task Handle(AddCommand command)
        {
            var lastWorkerItemOrder = await dbContext.WorkerItems
                .OrderByDescending(x => x.Order)
                .Select(x => x.Order)
                .FirstOrDefaultAsync();
            var newOrder = lastWorkerItemOrder + 1;
            WorkerItem workerItem = new()
            {
                Url = command.Url,
                Order = newOrder
            };
            
            await dbContext.WorkerItems.AddAsync(workerItem);
            await dbContext.SaveChangesAsync();
            
            var listMessages = dbContext.SentMessages
                .Where(x => x.Type == MessageType.LIST)
                .AsAsyncEnumerable();
            var workerItems = await dbContext.WorkerItems
                .OrderBy(x => x.Order)
                .ToListAsync();
            var lines = workerItems.Select(x => x.ToString());
            var text = workerItems.Count == 0 ? "<empty>" : string.Join('\n', lines);
            
            await foreach (var message in listMessages)
            {
                await client.Messages_EditMessage(
                    peer: new InputPeerSelf(),
                    id: message.TelegramId,
                    message: text
                );
            }

            await client.DeleteMessages(
                peer: new InputPeerSelf(),
                command.MessageId
            );
        }
    }
}