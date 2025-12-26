#if DEBUG
namespace pricewatcheruserbot.Configuration.Impl;

public class EnvUserInputProvider : IUserInputProvider
{
    public Task Init()
    {
        DotNetEnv.Env.Load();
        DotNetEnv.Env.TraversePath();

        return Task.CompletedTask;
    }

    public Task<int> Telegram_GetApiId() => Task.FromResult(EnvironmentVariables.TelegramApiId);

    public Task<string> Telegram_GetApiHash() => Task.FromResult(EnvironmentVariables.TelegramApiHash);

    public Task<string> Telegram_GetPassword() => Task.FromResult(EnvironmentVariables.TelegramPassword);
    public Task<string> Telegram_GetPhoneNumber() => Task.FromResult(EnvironmentVariables.TelegramPhoneNumber);

    public Task<string> Telegram_GetVerificationCode()
    {
        Console.Write("Enter telegram verification code: "); var result = Console.ReadLine()!; Console.WriteLine();

        return Task.FromResult(result);
    }

    public Task<string> Ozon_GetPhoneNumber() => Task.FromResult(EnvironmentVariables.OzonPhoneNumber);

    public Task<string> Ozon_GetEmailVerificationCode()
    {
        Console.Write("Enter ozon email verification code: "); var result = Console.ReadLine() ?? string.Empty; Console.WriteLine();

        return Task.FromResult(result);
    }

    public Task<string> Ozon_GetPhoneVerificationCode()
    {
        Console.Write("Enter ozon phone verification code: "); var result = Console.ReadLine() ?? string.Empty; Console.WriteLine();

        return Task.FromResult(result);
    }
}
#endif