namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedMoreDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movies", "Rating", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Movies", "Actors", c => c.String());
            AddColumn("dbo.Movies", "TrailerUrl", c => c.String());
            AddColumn("dbo.Movies", "Language", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movies", "Language");
            DropColumn("dbo.Movies", "TrailerUrl");
            DropColumn("dbo.Movies", "Actors");
            DropColumn("dbo.Movies", "Rating");
        }
    }
}