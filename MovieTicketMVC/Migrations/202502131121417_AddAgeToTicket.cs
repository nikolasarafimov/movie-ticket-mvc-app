namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAgeToTicket : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tickets", "AgeCategory", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tickets", "AgeCategory");
        }
    }
}