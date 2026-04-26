using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeReportGenerationData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntroductionMainGoal",
                table: "production_practice_student_assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationAddress",
                table: "production_practice_student_assignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationFullName",
                table: "production_practice_student_assignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationShortName",
                table: "production_practice_student_assignments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvidedMaterialsDescription",
                table: "production_practice_student_assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentDuties",
                table: "production_practice_student_assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkScheduleDescription",
                table: "production_practice_student_assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE production_practice_student_assignments
                SET "OrganizationFullName" = "OrganizationName",
                    "OrganizationShortName" = "OrganizationName"
                WHERE "OrganizationName" IS NOT NULL
                  AND ("OrganizationFullName" IS NULL OR "OrganizationFullName" = '')
                """);

            migrationBuilder.CreateTable(
                name: "production_practice_general_competencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeId = table.Column<int>(type: "integer", nullable: false),
                    CompetencyCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompetencyDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_practice_general_competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_practice_general_competencies_production_practic~",
                        column: x => x.ProductionPracticeId,
                        principalTable: "production_practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_general_competencies_ProductionPractice~",
                table: "production_practice_general_competencies",
                columns: new[] { "ProductionPracticeId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_practice_general_competencies");

            migrationBuilder.DropColumn(
                name: "IntroductionMainGoal",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationAddress",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationFullName",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationShortName",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "ProvidedMaterialsDescription",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "StudentDuties",
                table: "production_practice_student_assignments");

            migrationBuilder.DropColumn(
                name: "WorkScheduleDescription",
                table: "production_practice_student_assignments");
        }
    }
}
