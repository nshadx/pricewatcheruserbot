using Microsoft.Data.Sqlite;

namespace pricewatcheruserbot.Services;

public enum SentMessageType
{
    List
}

public class SentMessageService
{
    private readonly SqliteCommand _deleteAllCommand;
    private readonly SqliteCommand _addCommand;

    public SentMessageService(SqliteConnection connection)
    {
        _deleteAllCommand = new("""
                                DELETE FROM sent_messages WHERE type = @p1 RETURNING message_id
                                """, connection);
        _deleteAllCommand.Parameters.Add("@p1", SqliteType.Integer);

        _addCommand = new("""INSERT INTO sent_messages(message_id, type) VALUES (@p1, @p2)""", connection);
        _addCommand.Parameters.Add("@p1", SqliteType.Integer);
        _addCommand.Parameters.Add("@p2", SqliteType.Integer);
    }

    public IEnumerable<int> DeleteAll(SentMessageType type)
    {
        _deleteAllCommand.Parameters["@p1"].Value = (int)type;
        using (var reader = _deleteAllCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                yield return reader.GetInt32(0);
            }
        }
    }

    public void Add(int messageId, SentMessageType sentMessageType)
    {
        _addCommand.Parameters["@p1"].Value = messageId;
        _addCommand.Parameters["@p2"].Value = (int)sentMessageType;
        _addCommand.ExecuteNonQuery();
    }
}