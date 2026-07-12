using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Goal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "task_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "task_definitions");
        }
    }
}
