using System.Net.Http.Headers;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Notifications;

namespace PracticeMonitoring.Web.Services;

public class NotificationApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NotificationsPanelViewModel> GetNotificationsAsync(string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/notifications", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new NotificationsPanelViewModel();

        var json = await response.Content.ReadAsStringAsync();
        return new NotificationsPanelViewModel
        {
            Items = JsonSerializer.Deserialize<List<NotificationItemViewModel>>(json, _jsonOptions)
                    ?? new List<NotificationItemViewModel>()
        };
    }

    public async Task MarkAsReadAsync(string token, int id)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/notifications/{id}/read", token);
        await _httpClient.SendAsync(request);
    }

    public async Task MarkAllAsReadAsync(string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "api/notifications/read-all", token);
        await _httpClient.SendAsync(request);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
