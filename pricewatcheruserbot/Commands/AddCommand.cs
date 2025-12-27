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
        MessageManager messageManager
    )
    {
        public async Task Handle(AddCommand command)
        {
            var newOrder = await dbContext.WorkerItems.MaxAsync(x => (int?)x.Order) + 1 ?? 1;

            await dbContext.WorkerItems.AddAsync(new WorkerItem
            {
                Url = command.Url,
                Order = newOrder
            });
            await dbContext.SaveChangesAsync();
            
            await messageManager.UpdateAllLists();
        }
    }
}