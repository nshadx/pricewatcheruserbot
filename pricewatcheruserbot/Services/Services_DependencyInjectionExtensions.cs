using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;

namespace pricewatcheruserbot.Services;

public static class Services_DependencyInjectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDb()
        {
            builder.Services.AddSingleton<DbService>();
            builder.Services.AddSingleton(
                new SqliteConnection(builder.Configuration.GetConnectionString("DbConnection"))
            );
            builder.Services.AddFluentMigratorCore().ConfigureRunner(x =>
            {
                x.AddSQLite();
                x.WithGlobalConnectionString(builder.Configuration.GetConnectionString("DbConnection"));
                x.ScanIn(typeof(Program).Assembly);
            });

            return builder;
        }
        
        public IHostApplicationBuilder AddServices()
        {
            builder.Services.AddSingleton<SentMessageService>();
            builder.Services.AddSingleton<UserAgentService>();
            builder.Services.AddSingleton<WorkerItemService>();
            
            return builder;
        }
    }
}