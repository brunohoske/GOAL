using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Goal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalJoinCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "goals",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            // Backfill existing goals with random codes so the unique index can be created.
            migrationBuilder.Sql(
                """UPDATE goals SET "JoinCode" = upper(substr(md5(random()::text || "Id"::text), 1, 6)) WHERE "JoinCode" = '';""");

            migrationBuilder.CreateIndex(
                name: "IX_goals_JoinCode",
                table: "goals",
                column: "JoinCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_goals_JoinCode",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "goals");
        }
    }
}
