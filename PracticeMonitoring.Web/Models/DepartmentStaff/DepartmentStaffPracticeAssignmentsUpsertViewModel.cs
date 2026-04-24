using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPracticeAssignmentsUpsertViewModel : IValidatableObject
{
    public int PracticeId { get; set; }

    public List<DepartmentStaffPracticeStudentAssignmentEditViewModel> StudentAssignments { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var i = 0; i < StudentAssignments.Count; i++)
        {
            var assignment = StudentAssignments[i];
            if (assignment.StudentId <= 0)
                yield return new ValidationResult("Выберите студента.", new[] { $"StudentAssignments[{i}].StudentId" });
        }
    }
}
