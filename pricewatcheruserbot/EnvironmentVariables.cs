namespace pricewatcheruserbot;

public static class EnvironmentVariables
{
    public static string StorageDirectoryPath { get; } = GetRequiredEnvironmentVariable("StorageDirectoryPath");
    public static string DbConnectionString { get; } = GetRequiredEnvironmentVariable("DbConnectionString");
    public static string TelegramSessionFilePath { get; } = GetRequiredEnvironmentVariable("TelegramSessionFilePath");
    public static string BrowserSessionFilePath { get; } = GetRequiredEnvironmentVariable("BrowserSessionFilePath");
    
    private static string GetRequiredEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}