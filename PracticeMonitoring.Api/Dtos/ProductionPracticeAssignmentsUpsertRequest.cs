namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeAssignmentsUpsertRequest
{
    public List<ProductionPracticeStudentAssignmentRequest> StudentAssignments { get; set; } = new();
}
