namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedUrl : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Movies", "TrailerUrl");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Movies", "TrailerUrl", c => c.String());
        }
    }
}