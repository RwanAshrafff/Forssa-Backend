using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forssa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsAndSuccessStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "success_stories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: true),
                    achievement = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_success_stories", x => x.id);
                    table.ForeignKey(
                        name: "FK_success_stories_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_success_stories_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_success_stories_course_id",
                table: "success_stories",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_success_stories_user_id",
                table: "success_stories",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "success_stories");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Users");
        }
    }
}
