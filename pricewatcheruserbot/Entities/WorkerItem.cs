namespace pricewatcheruserbot.Entities;

public class WorkerItem
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Url { get; set; } = null!;

    public override string ToString() => $"{Order}. {Url}";
}