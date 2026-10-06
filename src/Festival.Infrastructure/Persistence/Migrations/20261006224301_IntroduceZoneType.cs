using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Festival.Infrastructure.Persistence.Migrations
{
    public partial class IntroduceZoneType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-persistent-deployment schema evolution: experimental Zone rows
            // are not preserved. Development databases with old rows need recreation.
            migrationBuilder.DropColumn(
                name: "IsFrontStanding",
                table: "Zones");

            migrationBuilder.AddColumn<string>(
                name: "ZoneType",
                table: "Zones",
                type: "text",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFrontStanding",
                table: "Zones",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Zones" SET "IsFrontStanding" = ("ZoneType" = 'FrontStanding');
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsFrontStanding",
                table: "Zones",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ZoneType",
                table: "Zones");
        }
    }
}
