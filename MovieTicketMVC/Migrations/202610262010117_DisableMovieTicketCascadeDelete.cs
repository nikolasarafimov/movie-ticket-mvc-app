namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DisableMovieTicketCascadeDelete : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Tickets", "MovieId", "dbo.Movies");
            AddForeignKey("dbo.Tickets", "MovieId", "dbo.Movies", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Tickets", "MovieId", "dbo.Movies");
            AddForeignKey("dbo.Tickets", "MovieId", "dbo.Movies", "Id", cascadeDelete: true);
        }
    }
}