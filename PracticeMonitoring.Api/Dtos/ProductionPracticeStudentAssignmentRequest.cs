using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeStudentAssignmentRequest
{
    [Required(ErrorMessage = "Выберите студента.")]
    public int StudentId { get; set; }

    public int? SupervisorId { get; set; }
}