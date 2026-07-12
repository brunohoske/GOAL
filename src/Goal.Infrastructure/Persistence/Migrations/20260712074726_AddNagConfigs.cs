using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Goal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNagConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RandomOverlayDaysBefore",
                table: "goal_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RandomOverlayEnabled",
                table: "goal_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TypingSabotageDaysBefore",
                table: "goal_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TypingSabotageEnabled",
                table: "goal_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TypingSabotageText",
                table: "goal_settings",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomOverlayDaysBefore",
                table: "goal_settings");

            migrationBuilder.DropColumn(
                name: "RandomOverlayEnabled",
                table: "goal_settings");

            migrationBuilder.DropColumn(
                name: "TypingSabotageDaysBefore",
                table: "goal_settings");

            migrationBuilder.DropColumn(
                name: "TypingSabotageEnabled",
                table: "goal_settings");

            migrationBuilder.DropColumn(
                name: "TypingSabotageText",
                table: "goal_settings");
        }
    }
}
