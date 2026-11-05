using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountMeIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interest_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pitch = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    ClosesOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    DoorsOpen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DoorsClose = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SpotsReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VenueRevealAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextQueueNumber = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interest_checks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false),
                    HasChangedMind = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstDoorAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LetInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SpotReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_participants_interest_checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "interest_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "participation_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participation_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_participation_changes_interest_checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "interest_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interest_checks_ReleaseAt",
                table: "interest_checks",
                column: "ReleaseAt",
                filter: "\"SpotsReleasedAt\" IS NULL AND \"ReleaseAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_interest_checks_ShareId",
                table: "interest_checks",
                column: "ShareId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participants_CheckId_PersonId",
                table: "participants",
                columns: new[] { "CheckId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participants_CheckId_Status_QueueNumber",
                table: "participants",
                columns: new[] { "CheckId", "Status", "QueueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_participation_changes_CheckId_Day",
                table: "participation_changes",
                columns: new[] { "CheckId", "Day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "participation_changes");

            migrationBuilder.DropTable(
                name: "interest_checks");
        }
    }
}
