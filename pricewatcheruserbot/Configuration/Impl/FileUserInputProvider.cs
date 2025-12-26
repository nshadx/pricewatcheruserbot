using System.Text.Json;

namespace pricewatcheruserbot.Configuration.Impl;

public class FileUserInputProvider : IUserInputProvider
{
    private const string Json = """
                                {
                                    "TelegramApiId": "",
                                    "TelegramApiHash": "",
                                    "TelegramPassword": "",
                                    "TelegramPhoneNumber": "",
                                    "TelegramVerificationCode": "",
                                    "OzonPhoneNumber": "",
                                    "OzonEmailVerificationCode": "",
                                    "OzonPhoneVerificationCode": ""
                                }
                                """;
    
    private static readonly string JsonFilePath = Path.Combine(EnvironmentVariables.StorageDirectoryPath, "exchanger.json");
    
    public async Task Init()
    {
        if (!File.Exists(JsonFilePath))
        {
            await File.WriteAllTextAsync(JsonFilePath, Json);
        }
        else
        {
            var text = await File.ReadAllTextAsync(JsonFilePath);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            dictionary?["TelegramVerificationCode"] = string.Empty;
            dictionary?["OzonPhoneVerificationCode"] = string.Empty;
            dictionary?["OzonEmailVerificationCode"] = string.Empty;
        }
    }

    public async Task<int> Telegram_GetApiId()
    {
        var text = await WaitForProperty("TelegramApiId");

        return int.Parse(text);
    }

    public Task<string> Telegram_GetApiHash() => WaitForProperty("TelegramApiHash");

    public Task<string> Telegram_GetPassword() => WaitForProperty("TelegramPassword");

    public Task<string> Telegram_GetPhoneNumber() => WaitForProperty("TelegramPhoneNumber");

    public Task<string> Telegram_GetVerificationCode() => WaitForProperty("TelegramVerificationCode");

    public Task<string> Ozon_GetPhoneNumber() => WaitForProperty("OzonPhoneNumber");

    public Task<string> Ozon_GetEmailVerificationCode() => WaitForProperty("OzonEmailVerificationCode");

    public Task<string> Ozon_GetPhoneVerificationCode() => WaitForProperty("OzonPhoneVerificationCode");

    private async Task<string> WaitForProperty(string name)
    {
        var dict = await Read();
        var value = dict[name];
        while (string.IsNullOrEmpty(value))
        {
            dict = await Read();
            value = dict[name];
            await Task.Delay(1000);
        }
        
        async Task<Dictionary<string, string>> Read()
        {
            try
            {
                var text = await File.ReadAllTextAsync(JsonFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(text)!;

                return dict;
            }
            catch
            {
                return [];
            }
        }

        return value;
    }
}