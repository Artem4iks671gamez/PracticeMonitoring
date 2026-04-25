namespace PracticeMonitoring.Web.Models.Messaging;

public class ChatApiResult<T>
{
    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public T? Data { get; set; }
}
