using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountMeIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CapacityDoorsAndCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinedOn",
                table: "participants");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedUpAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "interest_checks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DoorsClose",
                table: "interest_checks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DoorsOpen",
                table: "interest_checks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "SignedUpAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "interest_checks");

            migrationBuilder.DropColumn(
                name: "DoorsClose",
                table: "interest_checks");

            migrationBuilder.DropColumn(
                name: "DoorsOpen",
                table: "interest_checks");

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoinedOn",
                table: "participants",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
