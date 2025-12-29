namespace pricewatcheruserbot.Commands;

public static class Commands_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddCommandHandlers()
        {
            builder.Services.AddScoped<AddCommand.Handler>();
            builder.Services.AddScoped<ListCommand.Handler>();
            builder.Services.AddScoped<RemoveCommand.Handler>();

            return builder;
        }
    }
}