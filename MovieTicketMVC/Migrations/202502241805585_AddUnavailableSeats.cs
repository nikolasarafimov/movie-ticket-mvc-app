namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUnavailableSeats : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tickets", "UnavailableSeats", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tickets", "UnavailableSeats");
        }
    }
}