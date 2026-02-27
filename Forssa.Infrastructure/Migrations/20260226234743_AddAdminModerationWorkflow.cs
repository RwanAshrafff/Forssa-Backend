using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forssa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModerationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approval_status",
                table: "success_stories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "success_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "success_stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by_admin_email",
                table: "success_stories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_aid_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_url = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    reviewed_by_admin_email = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_aid_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_financial_aid_documents_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instructor_approval_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_position = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    reviewed_by_admin_email = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instructor_approval_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_instructor_approval_requests_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_aid_documents_user_id_status",
                table: "financial_aid_documents",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_instructor_approval_requests_user_id_status",
                table: "instructor_approval_requests",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_aid_documents");

            migrationBuilder.DropTable(
                name: "instructor_approval_requests");

            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "success_stories");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "success_stories");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "success_stories");

            migrationBuilder.DropColumn(
                name: "reviewed_by_admin_email",
                table: "success_stories");
        }
    }
}
