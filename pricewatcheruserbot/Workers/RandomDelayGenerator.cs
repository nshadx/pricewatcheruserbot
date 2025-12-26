namespace pricewatcheruserbot.Workers;

public static class RandomDelayGenerator
{
    private static readonly Random _random = Random.Shared;

    public static Task NextDelay(
        int minMinutes = 10,
        int maxMinutes = 15)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minMinutes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMinutes, minMinutes);

        var minutes = _random.Next(minMinutes, maxMinutes + 1);
        var seconds = _random.Next(0, 60);

        var delay = new TimeSpan(0, minutes, seconds);

        return Task.Delay(delay);
    }
}