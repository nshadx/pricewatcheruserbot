using System.Diagnostics.CodeAnalysis;
using pricewatcheruserbot.Services;
using pricewatcheruserbot.Utils;

namespace pricewatcheruserbot.Browser;

public class UserAgentProvider(AppDbContext dbContext)
{
    private string? _osName;
    
    public async Task<string> GetRandomUserAgent(string browserName)
    {
        await Initialize();

        var enumerable = dbContext.UserAgents
            .AsAsyncEnumerable()
            .Shuffle(window: 128)
            .Select(x => x.Value)
            .Where(x => x.Contains(browserName) && x.Contains(_osName));

        var value = await enumerable.FirstAsync();
        
        return value;
    }
    
    [MemberNotNull(nameof(_osName))]
    private Task Initialize()
    {
        _osName ??= OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : throw new PlatformNotSupportedException("Unsupported operating system");

        return Task.CompletedTask;
    }
}