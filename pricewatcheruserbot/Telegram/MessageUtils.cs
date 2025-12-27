namespace pricewatcheruserbot.Telegram;

public static class MessageUtils
{
    private static readonly string[] emojis =
    [
        "🔥", "🎉", "💥", "✨", "🌟", "🚀", "❤️", "😎", "🤩", "🌈",
        "💫", "🎊", "💎", "🎵", "🕺", "🍕", "🍿", "⚡", "🥳", "👑",
        "😄", "😁", "😂", "🤣", "😊", "😉", "😋", "😜", "🤪", "🤗",
        "😍", "🥰", "😘", "😎", "🤓", "🧐", "🤠", "🥸", "😈", "👻",
        "💀", "☠️", "👽", "🤖", "🎃", "😺", "😸", "😹", "😻", "😼",
        "🙌", "👏", "🤝", "👍", "👎", "✌️", "🤞", "🤟", "🤘", "👌",
        "💪", "🦾", "🧠", "🫀", "🫶", "💃", "🕺", "👯", "🏃", "🏆",
        "🎯", "🎮", "🕹️", "🎲", "♟️", "🎸", "🥁", "🎤", "🎧", "📸",
        "📱", "💻", "🖥️", "⌨️", "🖱️", "🧨", "💣", "🔔", "🎁", "🎈"
    ];

    
    public static string GenerateRandomEmojis(int count)
    {
        var chars = new char[count];

        for (var i = 0; i < count; i++)
        {
            var index = Random.Shared.Next(emojis.Length);
            chars[i] = emojis[index][0];
        }

        return new string(chars);
    }
}