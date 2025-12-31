using Microsoft.Data.Sqlite;

namespace pricewatcheruserbot.Services;

public class UserAgentService
{
    private readonly SqliteCommand _getAllUserAgentsCommand;
    private readonly SqliteCommand _insertCommand;
    private readonly SqliteCommand _deduplicationCommand; 

    public UserAgentService(SqliteConnection connection)
    {
        _getAllUserAgentsCommand = new("""
                                       SELECT value
                                       FROM user_agents
                                       WHERE value GLOB '*@p1*'
                                       AND value GLOB '*@p2*'
                                       """, connection);
        _getAllUserAgentsCommand.Parameters.Add("@p1", SqliteType.Text);
        _getAllUserAgentsCommand.Parameters.Add("@p2", SqliteType.Text);

        _insertCommand = new("""
                             INSERT INTO user_agents (value) VALUES (@p1)
                             """, connection);
        _insertCommand.Parameters.Add("@p1", SqliteType.Text);
        _deduplicationCommand = new("""
                                    WITH ranked AS (
                                        SELECT
                                            id,
                                            ROW_NUMBER() OVER (PARTITION BY value ORDER BY id) AS rn
                                        FROM user_agents
                                    )
                                    DELETE FROM user_agents
                                    WHERE id IN (
                                        SELECT id
                                        FROM ranked
                                        WHERE rn > 1
                                    );
                                    """, connection);
    }

    public IEnumerable<string> GetUserAgents(string osName, string browserName)
    {
        _getAllUserAgentsCommand.Parameters["@p1"].Value = osName;
        _getAllUserAgentsCommand.Parameters["@p2"].Value = browserName;

        using (var reader = _getAllUserAgentsCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                yield return reader.GetString(0);
            }
        }
    }

    public void Add(string value)
    {
        _insertCommand.Parameters["@p1"].Value = value;
        _insertCommand.ExecuteNonQuery();
    }

    public void Deduplicate()
    {
        _deduplicationCommand.ExecuteNonQuery();
    }
}