namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedLocalVideoToMovie : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Movies", "LocalTrailerPath", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Movies", "LocalTrailerPath");
        }
    }
}