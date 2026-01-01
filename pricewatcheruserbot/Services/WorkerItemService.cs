using System.Text.Json;
using Microsoft.Data.Sqlite;
using pricewatcheruserbot.Workers;

namespace pricewatcheruserbot.Services;

public record WorkerItem(int Id, string Url, string Name);

public class WorkerItemService
{
    private readonly SqliteCommand _removeCommand;
    private readonly SqliteCommand _insertCommand;
    private readonly SqliteCommand _getNamesCommand;
    private readonly SqliteCommand _getNameCommand;
    private readonly SqliteCommand _getAllCommand;
    private readonly SqliteCommand _getSmaCommand;
    private readonly SqliteCommand _updateSmaCommand;
    
    public WorkerItemService(SqliteConnection connection)
    {
        _insertCommand = new("""
                             INSERT INTO worker_items ("order", url)
                             VALUES (
                                 COALESCE(
                                     (SELECT MAX("order") + 1 FROM worker_items),
                                     1
                                 ),
                                 @p1
                             )
                             """, connection);
        _insertCommand.Parameters.Add("@p1", SqliteType.Integer);
        
        _removeCommand = new("""
                             BEGIN;
                             
                             DELETE FROM worker_items
                             WHERE "order" = @p1
                             RETURNING id;
                             
                             UPDATE worker_items
                             SET "order" = "order" - 1
                             WHERE "order" > @p1;
                             
                             COMMIT;
                             """, connection);
        _removeCommand.Parameters.Add("@p1", SqliteType.Integer);

        _getNamesCommand = new("""
                              SELECT "order" || '.' || ' ' || url
                              FROM worker_items
                              ORDER BY "order"
                              """, connection);
        
        _getNameCommand = new("""
                               SELECT "order" || '.' || ' ' || url
                               FROM worker_items
                               WHERE id = @p1
                               """, connection);
        _getNameCommand.Parameters.Add("@p1", SqliteType.Integer);
        
        _getAllCommand = new("""
                              SELECT id, url, "order" || '.' || ' ' || url
                              FROM worker_items
                              ORDER BY "order"
                              """, connection);

        _getSmaCommand = new("""
                             SELECT sma
                             FROM worker_items
                             WHERE id = @p1
                             """, connection);
        _getSmaCommand.Parameters.Add("@p1", SqliteType.Integer);

        _updateSmaCommand = new("""
                              UPDATE worker_items
                              SET sma = @p2
                              WHERE id = @p1
                              """, connection);
        _updateSmaCommand.Parameters.Add("@p1", SqliteType.Integer);
        _updateSmaCommand.Parameters.Add("@p2", SqliteType.Text);
    }

    public void Add(string url)
    {
        _insertCommand.Parameters["@p1"].Value = url;
        _insertCommand.ExecuteNonQuery();
    }

    public int Remove(int order)
    {
        _removeCommand.Parameters["@p1"].Value = order;
        using (var reader = _removeCommand.ExecuteReader())
        {
            reader.Read();
            
            return reader.GetInt32(0);
        }
    }

    public string GetName(int id)
    {
        _getNameCommand.Parameters["@p1"].Value = id;
        using (var reader = _getNameCommand.ExecuteReader())
        {
            reader.Read();
            return reader.GetString(0);
        }
    }
    
    public IEnumerable<string> GetNames()
    {
        using (var reader = _getNamesCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                yield return reader.GetString(0);
            }
        }
    }
    
    public IEnumerable<WorkerItem> GetAll()
    {
        using (var reader = _getAllCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var url = reader.GetString(1);
                var name = reader.GetString(2);
                
                yield return new(id, url, name);
            }
        }
    }

    public SimpleMovingAverage? GetSma(int id)
    {
        _getSmaCommand.Parameters["@p1"].Value = id;
        using (var reader = _getSmaCommand.ExecuteReader())
        {
            if (reader.Read() && !reader.IsDBNull(0))
            {
                var json = reader.GetStream(0);
                var state = JsonSerializer.Deserialize<SimpleMovingAverageState>(json);

                if (state is not null)
                { 
                    return new SimpleMovingAverage(state);
                }
            }

            return null;
        }
    }

    public void UpdateSma(int id, SimpleMovingAverage sma)
    {
        _updateSmaCommand.Parameters["@p1"].Value = id;
        
        var state = sma.Save();
        var json = JsonSerializer.Serialize(state);
        _updateSmaCommand.Parameters["@p2"].Value = json;

        _updateSmaCommand.ExecuteNonQuery();
    }
}