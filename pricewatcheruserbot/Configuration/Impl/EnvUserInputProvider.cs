namespace pricewatcheruserbot.Configuration.Impl;

public class EnvUserInputProvider : IUserInputProvider
{
    public EnvUserInputProvider()
    {
        DotNetEnv.Env.Load();
        DotNetEnv.Env.TraversePath();
    }
    
    public Task<int> Telegram_GetApiId() => Task.FromResult(EnvironmentVariables.TelegramApiId);
    public Task<string> Telegram_GetApiHash() => Task.FromResult(EnvironmentVariables.TelegramApiHash);
    public Task<string> Telegram_GetPassword() => Task.FromResult(EnvironmentVariables.TelegramPassword);
    public Task<string> Telegram_GetPhoneNumber() => Task.FromResult(EnvironmentVariables.TelegramPhoneNumber);

    public Task<string> Telegram_GetVerificationCode()
    {
        Console.Write("Enter telegram verification code (from Telegram Chat): "); var result = Console_ReadRequiredString(); Console.WriteLine();

        return Task.FromResult(result);
    }

    public Task<string> Ozon_GetPhoneNumber() => Task.FromResult(EnvironmentVariables.OzonPhoneNumber);

    public Task<string> Ozon_GetEmailVerificationCode()
    {
        Console.Write("Enter ozon email verification code: "); var result = Console_ReadRequiredString(); Console.WriteLine();

        return Task.FromResult(result);
    }

    public Task<string> Ozon_GetPhoneVerificationCode()
    {
        Console.Write("Enter ozon phone verification code (6 last digits): "); var result = Console_ReadRequiredString(); Console.WriteLine();

        return Task.FromResult(result);
    }

    private static string Console_ReadRequiredString()
    {
        var result = Console.ReadLine();

        if (string.IsNullOrEmpty(result))
        {
            throw new InvalidOperationException("Input text was not provided");
        }

        return result;
    }
}