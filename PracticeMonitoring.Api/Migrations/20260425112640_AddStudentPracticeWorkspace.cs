using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPracticeWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_practice_student_assignments_ProductionPracticeId",
                table: "production_practice_student_assignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "production_practice_student_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "production_practice_student_assignments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationSupervisorEmail",
                table: "production_practice_student_assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationSupervisorFullName",
                table: "production_practice_student_assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationSupervisorPhone",
                table: "production_practice_student_assignments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationSupervisorPosition",
                table: "production_practice_student_assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PracticeTaskContent",
                table: "production_practice_student_assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentDetailsUpdatedAtUtc",
                table: "production_practice_student_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_practice_appendices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeStudentAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_practice_appendices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_practice_appendices_production_practice_student_ass~",
                        column: x => x.ProductionPracticeStudentAssignmentId,
                        principalTable: "production_practice_student_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_practice_diary_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeStudentAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DetailedReport = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_practice_diary_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_practice_diary_entries_production_practice_student_~",
                        column: x => x.ProductionPracticeStudentAssignmentId,
                        principalTable: "production_practice_student_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_practice_report_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeStudentAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_practice_report_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_practice_report_items_production_practice_student_a~",
                        column: x => x.ProductionPracticeStudentAssignmentId,
                        principalTable: "production_practice_student_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_practice_sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeStudentAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_practice_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_practice_sources_production_practice_student_assign~",
                        column: x => x.ProductionPracticeStudentAssignmentId,
                        principalTable: "production_practice_student_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_student_assignments_ProductionPracticeI~",
                table: "production_practice_student_assignments",
                columns: new[] { "ProductionPracticeId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAtUtc",
                table: "notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_appendices_ProductionPracticeStudentAssign~",
                table: "student_practice_appendices",
                columns: new[] { "ProductionPracticeStudentAssignmentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_diary_entries_ProductionPracticeStudentAss~",
                table: "student_practice_diary_entries",
                columns: new[] { "ProductionPracticeStudentAssignmentId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_report_items_ProductionPracticeStudentAssi~",
                table: "student_practice_report_items",
                columns: new[] { "ProductionPracticeStudentAssignmentId", "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_sources_ProductionPracticeStudentAssignmen~",
                table: "student_practice_sources",
                columns: new[] { "ProductionPracticeStudentAssignmentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "student_practice_appendices");

            migrationBuilder.DropTable(
                name: "student_practice_diary_entries");

            migrationBuilder.DropTable(
                name: "student_practice_report_items");

            migrationBuilder.DropTable(
                name: "student_practice_sources");

            migrationBuilder.DropIndex(
                name: "IX_production_practice_student_assignments_ProductionPracticeI~",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationSupervisorEmail",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationSupervisorFullName",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationSupervisorPhone",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationSupervisorPosition",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "PracticeTaskContent",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "StudentDetailsUpdatedAtUtc",
                table: "production_practice_student_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_student_assignments_ProductionPracticeId",
                table: "production_practice_student_assignments",
                column: "ProductionPracticeId");
        }
    }
}
