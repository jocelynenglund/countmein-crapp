using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountMeIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VenueReveal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Venue",
                table: "interest_checks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VenueRevealAt",
                table: "interest_checks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Venue",
                table: "interest_checks");

            migrationBuilder.DropColumn(
                name: "VenueRevealAt",
                table: "interest_checks");
        }
    }
}
