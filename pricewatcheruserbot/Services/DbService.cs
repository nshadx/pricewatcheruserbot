using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;

namespace pricewatcheruserbot.Services;

public class DbService(IServiceProvider serviceProvider, SqliteConnection connection)
{
    public async Task Migrate()
    {
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }
        
        connection.Open();
    }
}