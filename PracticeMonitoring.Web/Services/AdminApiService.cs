using System.Net.Http.Headers;
using System.Text;
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
        => await GetLogsAsync("api/admin/logs/registered-users", token);

    public async Task<List<AdminLogItemViewModel>> GetAdminActionsLogsAsync(string token)
        => await GetLogsAsync("api/admin/logs/admin-actions", token);

    public async Task<List<AdminLogItemViewModel>> GetUserProfileChangesLogsAsync(string token)
        => await GetLogsAsync("api/admin/logs/user-profile-changes", token);

    public async Task<List<AdminUserItemViewModel>> GetUsersAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<AdminUserItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdminUserItemViewModel>>(json, _jsonOptions)
               ?? new List<AdminUserItemViewModel>();
    }

    public async Task<AdminApiResult<AdminUserItemViewModel>> UpdateUserAsync(string token, int id, object requestModel)
    {
        return await SendUserRequestAsync(HttpMethod.Put, $"api/admin/users/{id}", token, requestModel);
    }

    public async Task<AdminApiResult<AdminUserItemViewModel>> CreateAdminAsync(string token, object requestModel)
    {
        return await SendUserRequestAsync(HttpMethod.Post, "api/admin/users/create-admin", token, requestModel);
    }

    public async Task<AdminApiResult<AdminUserItemViewModel>> CreateSupervisorAsync(string token, object requestModel)
    {
        return await SendUserRequestAsync(HttpMethod.Post, "api/admin/users/create-supervisor", token, requestModel);
    }

    public async Task<AdminApiResult<AdminUserItemViewModel>> CreateDepartmentStaffAsync(string token, object requestModel)
    {
        return await SendUserRequestAsync(HttpMethod.Post, "api/admin/users/create-department-staff", token, requestModel);
    }

    private async Task<AdminApiResult<AdminUserItemViewModel>> SendUserRequestAsync(HttpMethod method, string url, string token, object requestModel)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestModel),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new AdminApiResult<AdminUserItemViewModel>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = JsonSerializer.Deserialize<AdminUserItemViewModel>(json, _jsonOptions)
            };
        }

        return new AdminApiResult<AdminUserItemViewModel>
        {
            Success = false,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = ExtractError(json) ?? "Не удалось выполнить операцию."
        };
    }

    private async Task<List<AdminLogItemViewModel>> GetLogsAsync(string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<AdminLogItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdminLogItemViewModel>>(json, _jsonOptions)
               ?? new List<AdminLogItemViewModel>();
    }

    private string? ExtractError(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
                return messageElement.GetString();
        }
        catch
        {
        }

        return null;
    }
}