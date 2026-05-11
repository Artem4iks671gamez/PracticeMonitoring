using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Supervisor;

namespace PracticeMonitoring.Web.Services;

public class SupervisorApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SupervisorApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SupervisorDashboardViewModel> GetDashboardAsync(string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/supervisor/dashboard", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new SupervisorDashboardViewModel();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupervisorDashboardViewModel>(json, _jsonOptions)
               ?? new SupervisorDashboardViewModel();
    }

    public async Task<SupervisorAssignmentDetailsViewModel?> GetAssignmentDetailsAsync(string token, int assignmentId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/supervisor/assignments/{assignmentId}", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupervisorAssignmentDetailsViewModel>(json, _jsonOptions);
    }

    public async Task<SupervisorAssignmentDetailsViewModel?> ReviewDiaryEntryAsync(
        string token,
        int entryId,
        SupervisorDiaryReviewRequestViewModel model)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/supervisor/diary-entries/{entryId}/review", token);
        request.Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupervisorAssignmentDetailsViewModel>(json, _jsonOptions);
    }

    public async Task<SupervisorAssignmentDetailsViewModel?> SaveSectionCommentAsync(
        string token,
        int assignmentId,
        string sectionKey,
        SupervisorSectionCommentRequestViewModel model)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/supervisor/assignments/{assignmentId}/section-comments/{Uri.EscapeDataString(sectionKey)}", token);
        request.Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupervisorAssignmentDetailsViewModel>(json, _jsonOptions);
    }

    public Task<SupervisorFileResult?> DownloadAppendixAsync(string token, int appendixId)
    {
        return DownloadFileAsync(token, $"api/supervisor/appendices/{appendixId}/download");
    }

    public Task<SupervisorFileResult?> DownloadDiaryAttachmentAsync(string token, int attachmentId)
    {
        return DownloadFileAsync(token, $"api/supervisor/diary-attachments/{attachmentId}/download");
    }

    private async Task<SupervisorFileResult?> DownloadFileAsync(string token, string url)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url, token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return new SupervisorFileResult
        {
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "file.bin"
        };
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
