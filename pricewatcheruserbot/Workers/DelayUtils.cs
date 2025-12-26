namespace pricewatcheruserbot.Workers;

public static class DelayUtils
{
    private static readonly Random _random = Random.Shared;
    private static readonly TimeSpan _producerDelay = TimeSpan.FromSeconds(30);
    
    public static Task RandomNext(
        int minMinutes = 10,
        int maxMinutes = 15,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minMinutes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMinutes, minMinutes);

        var minutes = _random.Next(minMinutes, maxMinutes + 1);
        var seconds = _random.Next(0, 60);

        var delay = new TimeSpan(0, minutes, seconds);

        return Task.Delay(delay, cancellationToken);
    }

    public static Task ProducerDelay(CancellationToken cancellationToken) => Task.Delay(_producerDelay, cancellationToken);
}