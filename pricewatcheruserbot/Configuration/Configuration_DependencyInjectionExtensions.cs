using pricewatcheruserbot.Configuration.Impl;

namespace pricewatcheruserbot.Configuration;

public static class Configuration_DependencyInjectionExtensions
{
    public static IServiceCollection AddUserInputProviders(this IServiceCollection services)
    {
#if DEBUG
        services.AddSingleton<IUserInputProvider, EnvUserInputProvider>();
#else
        services.AddSingleton<IUserInputProvider, FileUserInputProvider>();
#endif
        return services;
    }
}