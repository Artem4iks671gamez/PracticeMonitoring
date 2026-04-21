using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueFromSpecialtyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_specialties_Code",
                table: "specialties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_specialties_Code",
                table: "specialties",
                column: "Code",
                unique: true);
        }
    }
}
