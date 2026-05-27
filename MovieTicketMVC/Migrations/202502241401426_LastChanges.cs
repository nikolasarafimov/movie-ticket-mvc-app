namespace MovieTicketMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LastChanges : DbMigration
    {
        public override void Up()
        {
            Sql(@"
                IF COL_LENGTH('dbo.Tickets', 'TotalRows') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Tickets DROP COLUMN TotalRows
                END
            ");
            Sql(@"
                IF COL_LENGTH('dbo.Tickets', 'TotalCols') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Tickets DROP COLUMN TotalCols
                END
            ");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Tickets", "TotalCols", c => c.Int(nullable: false));
            AddColumn("dbo.Tickets", "TotalRows", c => c.Int(nullable: false));
        }
    }
}
