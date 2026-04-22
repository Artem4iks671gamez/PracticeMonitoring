namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffApiResult<T>
{
    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();

    public T? Data { get; set; }
}