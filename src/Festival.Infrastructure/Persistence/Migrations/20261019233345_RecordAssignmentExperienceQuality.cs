using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Festival.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordAssignmentExperienceQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-production structural transition. Recreate disposable databases
            // containing unclassified Assignments; never invent historical quality.
            migrationBuilder.AddColumn<string>(
                name: "ExperienceQuality",
                table: "Assignments",
                type: "text",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExperienceQuality",
                table: "Assignments");
        }
    }
}
