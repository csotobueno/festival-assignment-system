using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Festival.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneFrontStanding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFrontStanding",
                table: "Zones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing generic catalog entries have no Front Standing designation.
            // New zones must explicitly supply the marker.
            migrationBuilder.Sql(
                "ALTER TABLE \"Zones\" ALTER COLUMN \"IsFrontStanding\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFrontStanding",
                table: "Zones");
        }
    }
}
