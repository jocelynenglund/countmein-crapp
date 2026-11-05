using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountMeIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseUnclaimedSpots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DoorAttemptAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LetInAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SpotLostAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleasedSpots",
                table: "interest_checks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SpotsReleasedAt",
                table: "interest_checks",
                type: "timestamp with time zone",
                nullable: true);

            // Checks whose release moment has already passed when this ships: nobody's door attempts were
            // recorded, so there is no one to give spots to. Mark them as released with nothing taken away,
            // otherwise the worker would strip every unarrived holder's spot and hand it to nobody.
            migrationBuilder.Sql(@"
                UPDATE interest_checks
                SET ""SpotsReleasedAt"" = now()
                WHERE ""Capacity"" IS NOT NULL
                  AND ""DoorsOpen"" <= now()
                  AND ""DoorsClose"" - interval '10 minutes' <= now();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoorAttemptAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "LetInAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "SpotLostAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "ReleasedSpots",
                table: "interest_checks");

            migrationBuilder.DropColumn(
                name: "SpotsReleasedAt",
                table: "interest_checks");
        }
    }
}
