using System.Net.Http.Headers;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Admin;

namespace PracticeMonitoring.Web.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AdminLogItemViewModel>> GetRegisteredUsersLogsAsync(string token)
    {
        return await GetLogsAsync("api/admin/logs/registered-users", token);
    }

    public async Task<List<AdminLogItemViewModel>> GetAdminActionsLogsAsync(string token)
    {
        return await GetLogsAsync("api/admin/logs/admin-actions", token);
    }

    public async Task<List<AdminLogItemViewModel>> GetUserProfileChangesLogsAsync(string token)
    {
        return await GetLogsAsync("api/admin/logs/user-profile-changes", token);
    }

    private async Task<List<AdminLogItemViewModel>> GetLogsAsync(string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<AdminLogItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdminLogItemViewModel>>(json, _jsonOptions) ?? new List<AdminLogItemViewModel>();
    }
}