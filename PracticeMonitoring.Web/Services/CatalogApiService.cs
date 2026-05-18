using System.Text.Json;
using PracticeMonitoring.Web.Models.Catalog;

namespace PracticeMonitoring.Web.Services;

public class CatalogApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatalogApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CatalogSpecialtyOptionViewModel>> GetSpecialtiesAsync()
    {
        var response = await _httpClient.GetAsync("api/Helping/specialties");
        if (!response.IsSuccessStatusCode)
            return new List<CatalogSpecialtyOptionViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CatalogSpecialtyOptionViewModel>>(json, _jsonOptions)
               ?? new List<CatalogSpecialtyOptionViewModel>();
    }

    public async Task<List<CatalogGroupOptionViewModel>> GetGroupsAsync(int specialtyId)
    {
        var response = await _httpClient.GetAsync($"api/Helping/groups?specialtyId={specialtyId}");
        if (!response.IsSuccessStatusCode)
            return new List<CatalogGroupOptionViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CatalogGroupOptionViewModel>>(json, _jsonOptions)
               ?? new List<CatalogGroupOptionViewModel>();
    }
}
