using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.DepartmentStaff;

namespace PracticeMonitoring.Web.Services;

public class DepartmentStaffApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DepartmentStaffApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DepartmentStaffPracticeListItemViewModel>> GetPracticesAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/department-staff/practices");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<DepartmentStaffPracticeListItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DepartmentStaffPracticeListItemViewModel>>(json, _jsonOptions)
               ?? new List<DepartmentStaffPracticeListItemViewModel>();
    }

    public Task<List<DepartmentStaffAuditLogItemViewModel>> GetPracticeChangeLogsAsync(string token)
    {
        return GetLogsAsync(token, "api/department-staff/practice-logs/practice-changes");
    }

    public Task<List<DepartmentStaffAuditLogItemViewModel>> GetAssignmentChangeLogsAsync(string token)
    {
        return GetLogsAsync(token, "api/department-staff/practice-logs/assignment-changes");
    }

    public async Task<DepartmentStaffPracticeDetailsViewModel?> GetPracticeByIdAsync(string token, int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/department-staff/practices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DepartmentStaffPracticeDetailsViewModel>(json, _jsonOptions);
    }

    public async Task<List<DepartmentStaffSelectOptionViewModel>> GetSpecialtiesAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/department-staff/practices/metadata/specialties");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<DepartmentStaffSelectOptionViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DepartmentStaffSelectOptionViewModel>>(json, _jsonOptions)
               ?? new List<DepartmentStaffSelectOptionViewModel>();
    }

    public async Task<List<DepartmentStaffStudentOptionViewModel>> GetStudentsAsync(string token, int? specialtyId = null)
    {
        var url = specialtyId.HasValue && specialtyId.Value > 0
            ? $"api/department-staff/practices/metadata/students?specialtyId={specialtyId.Value}"
            : "api/department-staff/practices/metadata/students";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<DepartmentStaffStudentOptionViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DepartmentStaffStudentOptionViewModel>>(json, _jsonOptions)
               ?? new List<DepartmentStaffStudentOptionViewModel>();
    }

    public async Task<List<DepartmentStaffSupervisorOptionViewModel>> GetSupervisorsAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/department-staff/practices/metadata/supervisors");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<DepartmentStaffSupervisorOptionViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DepartmentStaffSupervisorOptionViewModel>>(json, _jsonOptions)
               ?? new List<DepartmentStaffSupervisorOptionViewModel>();
    }

    public async Task<DepartmentStaffApiResult<object>> SavePracticeAsync(string token, DepartmentStaffPracticeUpsertViewModel model)
    {
        var isEdit = model.Id.HasValue && model.Id.Value > 0;
        var method = isEdit ? HttpMethod.Put : HttpMethod.Post;
        var url = isEdit
            ? $"api/department-staff/practices/{model.Id!.Value}"
            : "api/department-staff/practices";

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(model),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new DepartmentStaffApiResult<object>
            {
                Success = true,
                StatusCode = (int)response.StatusCode
            };
        }

        return ParseErrorResult(json, (int)response.StatusCode, "Не удалось сохранить производственную практику.");
    }

    public async Task<DepartmentStaffApiResult<object>> SavePracticeAssignmentsAsync(string token, DepartmentStaffPracticeAssignmentsUpsertViewModel model)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/department-staff/practices/{model.PracticeId}/assignments");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(model),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new DepartmentStaffApiResult<object>
            {
                Success = true,
                StatusCode = (int)response.StatusCode
            };
        }

        return ParseErrorResult(json, (int)response.StatusCode, "Не удалось сохранить назначения студентов.");
    }

    public async Task<DepartmentStaffApiResult<object>> DeletePracticeAsync(string token, int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/department-staff/practices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new DepartmentStaffApiResult<object>
            {
                Success = true,
                StatusCode = (int)response.StatusCode
            };
        }

        return ParseErrorResult(json, (int)response.StatusCode, "Не удалось удалить производственную практику.");
    }

    public async Task<DepartmentStaffFileResult?> DownloadLogsAsync(string token, string category)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/department-staff/practice-logs/export/{category}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsByteArrayAsync();
        var fileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                       ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                       ?? "logs.txt";

        return new DepartmentStaffFileResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain; charset=utf-8",
            FileName = fileName
        };
    }

    private async Task<List<DepartmentStaffAuditLogItemViewModel>> GetLogsAsync(string token, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<DepartmentStaffAuditLogItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DepartmentStaffAuditLogItemViewModel>>(json, _jsonOptions)
               ?? new List<DepartmentStaffAuditLogItemViewModel>();
    }

    private DepartmentStaffApiResult<object> ParseErrorResult(string json, int statusCode, string fallbackMessage)
    {
        var result = new DepartmentStaffApiResult<object>
        {
            Success = false,
            StatusCode = statusCode,
            ErrorMessage = fallbackMessage
        };

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
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        result.ValidationErrors[property.Name] = property.Value
                            .EnumerateArray()
                            .Select(x => x.GetString() ?? string.Empty)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();
                    }
                }
            }
        }
        catch
        {
        }

        return result;
    }

    private static string? ExtractFileName(string? rawFileName)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
            return null;

        return rawFileName.Trim('"');
    }
}
