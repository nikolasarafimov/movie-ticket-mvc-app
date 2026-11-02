namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTotalPriceToTicket : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tickets", "TotalPrice", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tickets", "TotalPrice");
        }
    }
}