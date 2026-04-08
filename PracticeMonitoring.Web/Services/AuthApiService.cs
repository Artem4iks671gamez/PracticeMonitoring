using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Auth;

namespace PracticeMonitoring.Web.Services;

public class AuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterViewModel model)
    {
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/Auth/register", content);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(responseJson, _jsonOptions);
    }

    public async Task<AuthResponse?> LoginAsync(LoginViewModel model)
    {
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/Auth/login", content);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(responseJson, _jsonOptions);
    }

    public async Task<CurrentUserViewModel?> GetCurrentUserAsync(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync("api/Auth/me");

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CurrentUserViewModel>(responseJson, _jsonOptions);
    }
}