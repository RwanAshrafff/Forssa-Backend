using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forssa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPointsAwardedToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_points_awarded",
                table: "enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_points_awarded",
                table: "enrollments");
        }
    }
}
