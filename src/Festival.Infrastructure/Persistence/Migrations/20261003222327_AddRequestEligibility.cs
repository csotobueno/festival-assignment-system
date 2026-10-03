using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Festival.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsFrontStanding",
                table: "AssignmentRequests",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Preserve the opportunity space of pre-eligibility requests only.
            // New requests must supply eligibility; there is no database default.
            migrationBuilder.Sql(
                "ALTER TABLE \"AssignmentRequests\" ALTER COLUMN \"AllowsFrontStanding\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsFrontStanding",
                table: "AssignmentRequests");
        }
    }
}
