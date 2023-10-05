using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Migrations
{
	/// <summary>
	/// This initial migration should always remain.
	/// It is used to set the desired database collation, regardless of whether the database had to be newly created or not.
	/// </summary>
    public partial class DatabaseCollation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.AlterDatabase(collation: CoreDbContext.DefaultCollation);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
