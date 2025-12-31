namespace pricewatcheruserbot.Utils;

public static class DelayUtils
{
    private static readonly TimeSpan _producerDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _userAgentFetchDelay = TimeSpan.FromMinutes(60);
    
    public static Task RandomNext(
        int minMinutes = 2,
        int maxMinutes = 5,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minMinutes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMinutes, minMinutes);

        var minutes = Random.Shared.Next(minMinutes, maxMinutes + 1);
        var seconds = Random.Shared.Next(0, 60);

        var delay = new TimeSpan(0, minutes, seconds);

        return Task.Delay(delay, cancellationToken);
    }

    public static Task ProducerDelay(CancellationToken cancellationToken) => Task.Delay(_producerDelay, cancellationToken);
    public static Task UserAgentFetchDelay(CancellationToken cancellationToken) => Task.Delay(_userAgentFetchDelay, cancellationToken);
}