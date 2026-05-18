namespace PracticeMonitoring.Api.Services;

public class UnisenderOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.unisender.com/ru/api";

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Practice Monitoring";

    public string ListId { get; set; } = string.Empty;

    public string ListTitle { get; set; } = "Practice Monitoring";
}
