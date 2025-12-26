namespace pricewatcheruserbot.Configuration;

public static class EnvironmentVariables
{
    public static string StorageDirectoryPath { get; } = Environment.GetEnvironmentVariable("StorageDirectoryPath") ?? AppContext.BaseDirectory;
    public static string DbConnectionString { get; } = GetRequiredEnvironmentVariable("DbConnectionString");
    public static string TelegramSessionFilePath { get; } = GetRequiredEnvironmentVariable("TelegramSessionFilePath");
    public static string BrowserSessionFilePath { get; } = GetRequiredEnvironmentVariable("BrowserSessionFilePath");

#if DEBUG
    public static int TelegramApiId { get; } = int.Parse(GetRequiredEnvironmentVariable("TelegramApiId"));
    public static string TelegramPhoneNumber { get; } = GetRequiredEnvironmentVariable("TelegramPhoneNumber");
    public static string TelegramApiHash { get; } = GetRequiredEnvironmentVariable("TelegramApiHash");
    public static string TelegramPassword { get; } = GetRequiredEnvironmentVariable("TelegramPassword");
    public static string OzonPhoneNumber { get; } = GetRequiredEnvironmentVariable("OzonPhoneNumber");
#endif
    
    private static string GetRequiredEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}