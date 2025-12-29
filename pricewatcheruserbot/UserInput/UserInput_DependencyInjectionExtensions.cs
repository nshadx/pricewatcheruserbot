namespace pricewatcheruserbot.UserInput;

public static class UserInput_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddUserInput()
        {
            builder.Services.AddSingleton<IUserInput, ConsoleUserInput>();
            
            return builder;
        }
    }
}