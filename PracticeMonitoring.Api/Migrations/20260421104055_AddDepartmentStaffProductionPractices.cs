using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentStaffProductionPractices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "production_practices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PracticeIndex = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false),
                    ProfessionalModuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProfessionalModuleName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Hours = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_practices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_practices_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_practice_competencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeId = table.Column<int>(type: "integer", nullable: false),
                    CompetencyCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompetencyDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorkTypes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Hours = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_practice_competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_practice_competencies_production_practices_Produ~",
                        column: x => x.ProductionPracticeId,
                        principalTable: "production_practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_practice_student_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionPracticeId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    SupervisorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_practice_student_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_practice_student_assignments_production_practice~",
                        column: x => x.ProductionPracticeId,
                        principalTable: "production_practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_practice_student_assignments_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_practice_student_assignments_users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_competencies_ProductionPracticeId",
                table: "production_practice_competencies",
                column: "ProductionPracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_student_assignments_ProductionPracticeId",
                table: "production_practice_student_assignments",
                column: "ProductionPracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_student_assignments_StudentId",
                table: "production_practice_student_assignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_production_practice_student_assignments_SupervisorId",
                table: "production_practice_student_assignments",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_production_practices_SpecialtyId",
                table: "production_practices",
                column: "SpecialtyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_practice_competencies");

            migrationBuilder.DropTable(
                name: "production_practice_student_assignments");

            migrationBuilder.DropTable(
                name: "production_practices");
        }
    }
}
