namespace pricewatcheruserbot.Commands;

public static class Commands_DependencyInjectionExtensions
{
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddScoped<AddCommand.Handler>();
        services.AddScoped<ListCommand.Handler>();
        services.AddScoped<RemoveCommand.Handler>();

        return services;
    }
}