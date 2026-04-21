using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Admin;
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
        return JsonSerializer.Deserialize<List<DepartmentStaffPracticeListItemViewModel>>(json, _jsonOptions) ?? new();
    }

    public async Task<DepartmentStaffPracticeDetailsViewModel?> GetPracticeAsync(string token, int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/department-staff/practices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DepartmentStaffPracticeDetailsViewModel>(json, _jsonOptions);
    }

    public async Task<AdminApiResult<DepartmentStaffPracticeDetailsViewModel>> CreatePracticeAsync(
        string token,
        DepartmentStaffPracticeUpsertViewModel model)
    {
        return await SendPracticeRequestAsync(HttpMethod.Post, "api/department-staff/practices", token, model);
    }

    public async Task<AdminApiResult<DepartmentStaffPracticeDetailsViewModel>> UpdatePracticeAsync(
        string token,
        int id,
        DepartmentStaffPracticeUpsertViewModel model)
    {
        return await SendPracticeRequestAsync(HttpMethod.Put, $"api/department-staff/practices/{id}", token, model);
    }

    public async Task<AdminApiResult<object>> DeletePracticeAsync(string token, int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/department-staff/practices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new AdminApiResult<object>
            {
                Success = true,
                StatusCode = (int)response.StatusCode
            };
        }

        return new AdminApiResult<object>
        {
            Success = false,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = ExtractError(json) ?? "Не удалось удалить производственную практику."
        };
    }

    private async Task<AdminApiResult<DepartmentStaffPracticeDetailsViewModel>> SendPracticeRequestAsync(
        HttpMethod method,
        string url,
        string token,
        DepartmentStaffPracticeUpsertViewModel model)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            PracticeIndex = model.PracticeIndex,
            Name = model.Name,
            SpecialtyId = model.SpecialtyId,
            ProfessionalModuleCode = model.ProfessionalModuleCode,
            ProfessionalModuleName = model.ProfessionalModuleName,
            Hours = model.Hours,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Competencies = model.Competencies.Select(x => new
            {
                x.CompetencyCode,
                x.CompetencyDescription,
                x.WorkTypes,
                x.Hours
            }),
            StudentAssignments = model.StudentAssignments.Select(x => new
            {
                x.StudentId,
                x.SupervisorId
            })
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new AdminApiResult<DepartmentStaffPracticeDetailsViewModel>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = JsonSerializer.Deserialize<DepartmentStaffPracticeDetailsViewModel>(json, _jsonOptions)
            };
        }

        return new AdminApiResult<DepartmentStaffPracticeDetailsViewModel>
        {
            Success = false,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = ExtractError(json) ?? "Не удалось сохранить производственную практику."
        };
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