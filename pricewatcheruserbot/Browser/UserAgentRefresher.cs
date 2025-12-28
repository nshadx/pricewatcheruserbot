using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using pricewatcheruserbot.Configuration;
using pricewatcheruserbot.Utils;

namespace pricewatcheruserbot.Browser;

public class UserAgentRefresher(
    IEnumerable<IUserAgentFetcher> fetchers
) : BackgroundService
{
    private readonly SqliteCommand _insertCommand = new("""
                                                        INSERT INTO "UserAgents" ("Value") VALUES (@value)
                                                        """)
    {
        Parameters = { new SqliteParameter("@value", SqliteType.Text) }
    };

    private readonly SqliteCommand _deduplicationCommand = new("""
                                                               WITH ranked AS (
                                                                   SELECT
                                                                       "Id",
                                                                       ROW_NUMBER() OVER (PARTITION BY "Value" ORDER BY "Id") AS rn
                                                                   FROM "UserAgents"
                                                               )
                                                               DELETE FROM "UserAgents"
                                                               WHERE "Id" IN (
                                                                   SELECT "Id"
                                                                   FROM ranked
                                                                   WHERE rn > 1
                                                               );
                                                               """);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var source = AsyncEnumerable.Empty<string>().Merge(fetchers.Select(x => x.Enumerate()));
            
            var channel = Channel.CreateUnbounded<string>();

            var producerTask = ProducerTask(source, channel.Writer, stoppingToken);
            var consumerTask = ConsumerTask(channel.Reader, stoppingToken);
            
            await Task.WhenAll(producerTask, consumerTask);
            await DelayUtils.UserAgentFetchDelay(stoppingToken);
        }
    }

    public override void Dispose()
    {
        _insertCommand.Dispose();
        _deduplicationCommand.Dispose();
        base.Dispose();
    }

    private async Task ProducerTask(IAsyncEnumerable<string> source, ChannelWriter<string> channel, CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(source, stoppingToken, channel.WriteAsync);
        channel.Complete();
    }

    private async Task ConsumerTask(ChannelReader<string> channel, CancellationToken stoppingToken)
    {
        using (var connection = new SqliteConnection(EnvironmentVariables.DbConnectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                _insertCommand.Connection = connection;
                _insertCommand.Transaction = transaction;
                
                await foreach (var value in channel.ReadAllAsync(stoppingToken))
                {
                    _insertCommand.Parameters["@value"].Value = value;
                    _insertCommand.ExecuteNonQuery();
                }

                _deduplicationCommand.Connection = connection;
                _deduplicationCommand.Transaction = transaction;
                _deduplicationCommand.ExecuteNonQuery();

                transaction.Commit();
            }

            connection.Close();
        }
    }
}