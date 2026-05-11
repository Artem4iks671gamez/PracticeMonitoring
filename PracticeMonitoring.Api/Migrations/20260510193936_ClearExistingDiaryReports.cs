using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class ClearExistingDiaryReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM student_practice_diary_attachments;

                UPDATE student_practice_diary_entries
                SET
                    "DetailedReport" = '',
                    "IsReviewed" = FALSE,
                    "SupervisorGrade" = NULL,
                    "SupervisorComment" = NULL,
                    "ReviewedAtUtc" = NULL,
                    "ReviewedBySupervisorId" = NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
