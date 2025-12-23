namespace pricewatcheruserbot;

public static class EnvironmentVariables
{
    public static string StorageDirectoryPath { get; } = Environment.GetEnvironmentVariable("StorageDirectoryPath") ?? AppContext.BaseDirectory;
    public static string DbConnectionString { get; } = GetRequiredEnvironmentVariable("DbConnectionString");
    public static string TelegramSessionFilePath { get; } = GetRequiredEnvironmentVariable("TelegramSessionFilePath");
    public static string BrowserSessionFilePath { get; } = GetRequiredEnvironmentVariable("BrowserSessionFilePath");

#if DEBUG
    public static string PhoneNumber { get; } = GetRequiredEnvironmentVariable("phone_number");
    public static string ApiId { get; } = GetRequiredEnvironmentVariable("api_id");
    public static string ApiHash { get; } = GetRequiredEnvironmentVariable("api_hash");
    public static string Password { get; } = GetRequiredEnvironmentVariable("password");
#endif
    
    private static string GetRequiredEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}