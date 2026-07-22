using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Festival.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendees",
                columns: table => new
                {
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.AttendeeId);
                });

            migrationBuilder.CreateTable(
                name: "FestivalDays",
                columns: table => new
                {
                    FestivalDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AssignmentWindowStart = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    AssignmentWindowEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FestivalDays", x => x.FestivalDayId);
                    table.CheckConstraint("CK_FestivalDays_AssignmentWindow_StartBeforeEnd", "\"AssignmentWindowStart\" < \"AssignmentWindowEnd\"");
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.ZoneId);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentRequests",
                columns: table => new
                {
                    AssignmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FestivalDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RejectionMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentRequests", x => x.AssignmentRequestId);
                    table.ForeignKey(
                        name: "FK_AssignmentRequests_FestivalDays_FestivalDayId",
                        column: x => x.FestivalDayId,
                        principalTable: "FestivalDays",
                        principalColumn: "FestivalDayId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Spots",
                columns: table => new
                {
                    SpotCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SpotNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spots", x => x.SpotCode);
                    table.ForeignKey(
                        name: "FK_Spots_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentRequestAttendees",
                columns: table => new
                {
                    AssignmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    AttendeeCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentRequestAttendees", x => new { x.AssignmentRequestId, x.Position });
                    table.ForeignKey(
                        name: "FK_AssignmentRequestAttendees_AssignmentRequests_AssignmentReq~",
                        column: x => x.AssignmentRequestId,
                        principalTable: "AssignmentRequests",
                        principalColumn: "AssignmentRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FestivalDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpotCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SpotNumber = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_AssignmentRequests_AssignmentRequestId",
                        column: x => x.AssignmentRequestId,
                        principalTable: "AssignmentRequests",
                        principalColumn: "AssignmentRequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalTable: "Attendees",
                        principalColumn: "AttendeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_FestivalDays_FestivalDayId",
                        column: x => x.FestivalDayId,
                        principalTable: "FestivalDays",
                        principalColumn: "FestivalDayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Spots_SpotCode",
                        column: x => x.SpotCode,
                        principalTable: "Spots",
                        principalColumn: "SpotCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRequestAttendees_AssignmentRequestId_AttendeeCode",
                table: "AssignmentRequestAttendees",
                columns: new[] { "AssignmentRequestId", "AttendeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRequests_FestivalDayId_RequestedAt",
                table: "AssignmentRequests",
                columns: new[] { "FestivalDayId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentRequestId_AttendeeId",
                table: "Assignments",
                columns: new[] { "AssignmentRequestId", "AttendeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_FestivalDayId_AttendeeId",
                table: "Assignments",
                columns: new[] { "FestivalDayId", "AttendeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_FestivalDayId_SpotCode",
                table: "Assignments",
                columns: new[] { "FestivalDayId", "SpotCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_AttendeeCode",
                table: "Attendees",
                column: "AttendeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FestivalDays_Date",
                table: "FestivalDays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Spots_ZoneId_RowCode_SpotNumber",
                table: "Spots",
                columns: new[] { "ZoneId", "RowCode", "SpotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentRequestAttendees");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "AssignmentRequests");

            migrationBuilder.DropTable(
                name: "Attendees");

            migrationBuilder.DropTable(
                name: "Spots");

            migrationBuilder.DropTable(
                name: "FestivalDays");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
