using pricewatcheruserbot.Configuration.Impl;

namespace pricewatcheruserbot.Configuration;

public static class Configuration_DependencyInjectionExtensions
{
    public static IServiceCollection AddUserInputProviders(this IServiceCollection services)
    {
        services.AddSingleton<IUserInputProvider, EnvUserInputProvider>();
        
        return services;
    }
}