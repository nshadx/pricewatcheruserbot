namespace pricewatcheruserbot.Workers;

public static class MessageUtils
{
    public static string GenerateRandomEmojis(int count)
    {
        string[] emojis =
        [
            "🔥", "🎉", "💥", "✨", "🌟", "🚀", "❤️", "😎", "🤩", "🌈",
            "💫", "🎊", "💎", "🎵", "🕺", "🍕", "🍿", "⚡", "🥳", "👑"
        ];

        string result = "";

        for (var i = 0; i < count; i++)
        {
            int index = Random.Shared.Next(emojis.Length);
            result += emojis[index];
        }

        return result;
    }
}