using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CountMeIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EarlyBirds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "changed_minds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InterestCheckId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_changed_minds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_changed_minds_interest_checks_InterestCheckId",
                        column: x => x.InterestCheckId,
                        principalTable: "interest_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_changed_minds_InterestCheckId_PersonId",
                table: "changed_minds",
                columns: new[] { "InterestCheckId", "PersonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "changed_minds");
        }
    }
}
