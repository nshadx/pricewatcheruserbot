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
            .Where(x => x.Value.Contains(browserName) && x.Value.Contains(_osName))
            .Randomize()
            .Select(x => x.Value);

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