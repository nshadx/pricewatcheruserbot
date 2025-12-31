using System.Diagnostics.CodeAnalysis;
using pricewatcheruserbot.Services;

namespace pricewatcheruserbot.Browser;

public class UserAgentProvider(UserAgentService userAgentService)
{
    private const string _defaultUserAgentForChrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private string? _osName;
    
    public async Task<string> GetRandomUserAgent(string browserName)
    {
        await Initialize();

        var value = userAgentService.GetUserAgents(_osName, browserName)
            .Shuffle()
            .FirstOrDefault(_defaultUserAgentForChrome);
        return value;
    }
    
    [MemberNotNull(nameof(_osName))]
    private Task Initialize()
    {
        _osName ??= OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : throw new PlatformNotSupportedException("Unsupported operating system");

        return Task.CompletedTask;
    }
}