using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Student;

namespace PracticeMonitoring.Web.Services;

public class StudentApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StudentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<List<StudentPracticeListItemViewModel>> GetPracticesAsync(string token)
    {
        return GetListAsync<StudentPracticeListItemViewModel>(token, "api/student/practices");
    }

    public async Task<StudentPracticeDetailsViewModel?> GetPracticeAsync(string token, int assignmentId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/student/practices/{assignmentId}", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StudentPracticeDetailsViewModel>(json, _jsonOptions);
    }

    public Task<StudentApiResult<StudentPracticeDetailsViewModel>> SaveOrganizationAsync(
        string token,
        int assignmentId,
        StudentPracticeOrganizationRequestViewModel model)
    {
        return SendJsonAsync<StudentPracticeDetailsViewModel>(
            token,
            HttpMethod.Put,
            $"api/student/practices/{assignmentId}/organization",
            model,
            "Не удалось сохранить сведения об организации.");
    }

    public Task<StudentApiResult<StudentPracticeDetailsViewModel>> SaveDiaryEntryAsync(
        string token,
        int assignmentId,
        StudentPracticeDiaryEntryRequestViewModel model)
    {
        return SendJsonAsync<StudentPracticeDetailsViewModel>(
            token,
            HttpMethod.Put,
            $"api/student/practices/{assignmentId}/diary",
            model,
            "Не удалось сохранить запись дневника.");
    }

    public Task<StudentApiResult<StudentPracticeDetailsViewModel>> SaveReportItemsAsync(
        string token,
        int assignmentId,
        StudentPracticeReportItemsRequestViewModel model)
    {
        return SendJsonAsync<StudentPracticeDetailsViewModel>(
            token,
            HttpMethod.Put,
            $"api/student/practices/{assignmentId}/report-items",
            model,
            "Не удалось сохранить таблицы отчёта.");
    }

    public Task<StudentApiResult<StudentPracticeDetailsViewModel>> SaveSourcesAsync(
        string token,
        int assignmentId,
        StudentPracticeSourcesRequestViewModel model)
    {
        return SendJsonAsync<StudentPracticeDetailsViewModel>(
            token,
            HttpMethod.Put,
            $"api/student/practices/{assignmentId}/sources",
            model,
            "Не удалось сохранить источники.");
    }

    public async Task<StudentApiResult<StudentPracticeDetailsViewModel>> UploadAppendixAsync(
        string token,
        int assignmentId,
        string? title,
        string? description,
        IFormFile? file)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/student/practices/{assignmentId}/appendices", token);
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(title ?? string.Empty), "title");
        formData.Add(new StringContent(description ?? string.Empty), "description");

        if (file is not null && file.Length > 0)
        {
            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            formData.Add(streamContent, "file", file.FileName);
        }

        request.Content = formData;
        return await SendAsync<StudentPracticeDetailsViewModel>(request, "Не удалось загрузить приложение.");
    }

    public async Task<StudentApiResult<object>> DeleteAppendixAsync(string token, int appendixId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/student/appendices/{appendixId}", token);
        return await SendAsync<object>(request, "Не удалось удалить приложение.");
    }

    public async Task<StudentFileResult?> DownloadAppendixAsync(string token, int appendixId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/student/appendices/{appendixId}/download", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return new StudentFileResult
        {
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                       ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                       ?? "appendix.bin"
        };
    }

    public async Task<StudentFileResult?> DownloadDiaryAttachmentAsync(string token, int attachmentId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/student/diary-attachments/{attachmentId}/download", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return new StudentFileResult
        {
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                       ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                       ?? "figure.bin"
        };
    }

    private async Task<List<T>> GetListAsync<T>(string token, string url)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url, token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<T>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
    }

    private async Task<StudentApiResult<T>> SendJsonAsync<T>(
        string token,
        HttpMethod method,
        string url,
        object payload,
        string fallbackMessage)
    {
        using var request = CreateAuthorizedRequest(method, url, token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await SendAsync<T>(request, fallbackMessage);
    }

    private async Task<StudentApiResult<T>> SendAsync<T>(HttpRequestMessage request, string fallbackMessage)
    {
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        var result = new StudentApiResult<T>
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode
        };

        if (response.IsSuccessStatusCode)
        {
            result.Data = string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return result;
        }

        result.ErrorMessage = fallbackMessage;

        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
                result.ErrorMessage = messageElement.GetString() ?? fallbackMessage;

            if (doc.RootElement.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    result.ValidationErrors[property.Name] = property.Value
                        .EnumerateArray()
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }
            }
        }
        catch
        {
        }

        return result;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static string? ExtractFileName(string? rawFileName)
    {
        return string.IsNullOrWhiteSpace(rawFileName) ? null : rawFileName.Trim('"');
    }
}
