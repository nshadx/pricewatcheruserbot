using Microsoft.Data.Sqlite;

namespace pricewatcheruserbot.Services;

public enum SentMessageType
{
    List,
    PriceDropped
}

public class SentMessageService
{
    private readonly SqliteCommand _deleteAllCommand;
    private readonly SqliteCommand _deleteCommand;
    private readonly SqliteCommand _addCommand;
    private readonly SqliteCommand _getAllCommand;

    public SentMessageService(SqliteConnection connection)
    {
        _deleteAllCommand = new("""
                                DELETE FROM sent_messages WHERE type = @p1 RETURNING message_id
                                """, connection);
        _deleteAllCommand.Parameters.Add("@p1", SqliteType.Integer);

        _addCommand = new("""INSERT INTO sent_messages(message_id, type, worker_item_id) VALUES (@p1, @p2, @p3)""", connection);
        _addCommand.Parameters.Add("@p1", SqliteType.Integer);
        _addCommand.Parameters.Add("@p2", SqliteType.Integer);
        _addCommand.Parameters.Add("@p3", SqliteType.Integer);

        _deleteCommand = new("""
                             DELETE FROM sent_messages WHERE type = @p1 AND worker_item_id = @p2 RETURNING message_id
                             """, connection);
        _deleteCommand.Parameters.Add("@p1", SqliteType.Integer);
        _deleteCommand.Parameters.Add("@p2", SqliteType.Integer);

        _getAllCommand = new("""SELECT message_id FROM sent_messages WHERE type = @p1""", connection);
        _getAllCommand.Parameters.Add("@p1", SqliteType.Integer);
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

    public IEnumerable<int> GetAll(SentMessageType type)
    {
        _getAllCommand.Parameters["@p1"].Value = (int)type;
        using (var reader = _getAllCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                yield return reader.GetInt32(0);
            }
        }
    }

    public int Delete(SentMessageType type, int workerItemId)
    {
        _deleteCommand.Parameters["@p1"].Value = (int)type;
        _deleteCommand.Parameters["@p2"].Value = workerItemId;
        using (var reader = _deleteCommand.ExecuteReader())
        {
            if (reader.Read())
            {
                return reader.GetInt32(0);
            }
        }

        return -1;
    }

    public void Add(int messageId, SentMessageType sentMessageType, int workerItemId = -1)
    {
        _addCommand.Parameters["@p1"].Value = messageId;
        _addCommand.Parameters["@p2"].Value = (int)sentMessageType;
        _addCommand.Parameters["@p3"].Value = workerItemId == -1 ? DBNull.Value : workerItemId;
        _addCommand.ExecuteNonQuery();
    }
}