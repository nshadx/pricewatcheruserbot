using FluentMigrator;

namespace pricewatcheruserbot.Migrations;

[Migration(20251231)]
public class InitialMigration : Migration
{
    public override void Up()
    {
        Create.Table("sent_messages")
            .WithColumn("id").AsInt32().Identity().PrimaryKey()
            .WithColumn("message_id").AsInt32().NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("worker_item_id").AsInt32().ForeignKey("worker_items", "id").Nullable();
        Create.Table("worker_items")
            .WithColumn("id").AsInt32().Identity().PrimaryKey()
            .WithColumn("order").AsInt32().NotNullable()
            .WithColumn("url").AsString().NotNullable()
            .WithColumn("sma").AsString().Nullable();
        Create.Table("user_agents")
            .WithColumn("id").AsInt32().Identity().PrimaryKey()
            .WithColumn("value").AsString().NotNullable();

        Create.Index("IX_sent_messages_type").OnTable("sent_messages").OnColumn("type");
        Create.Index("IX_worker_items_order").OnTable("worker_items").OnColumn("order");
        Create.Index("IX_user_agents_value").OnTable("user_agents").OnColumn("value");
    }

    public override void Down()
    {
        Delete.Table("sent_messages");
        Delete.Table("worker_items");
        Delete.Table("user_agents");
    }
}