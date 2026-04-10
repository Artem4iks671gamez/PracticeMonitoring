namespace PracticeMonitoring.Web.Models.Admin;

public class AdminApiResult<T>
{
    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public T? Data { get; set; }
}