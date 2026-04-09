using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models;
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

    public async Task<AuthApiResult<AuthResponse>> RegisterAsync(RegisterViewModel model)
    {
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/Auth/register", content);
        return await ReadAuthResponseAsync<AuthResponse>(response, "Ошибка регистрации.");
    }

    public async Task<AuthApiResult<AuthResponse>> LoginAsync(LoginViewModel model)
    {
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/Auth/login", content);
        return await ReadAuthResponseAsync<AuthResponse>(response, "Ошибка входа.");
    }

    public async Task<CurrentUserViewModel?> GetCurrentUserAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CurrentUserViewModel>(responseJson, _jsonOptions);
    }

    public async Task<AuthApiResult<CurrentUserViewModel>> UpdateProfileAsync(string token, UpdateProfileRequest requestModel)
    {
        var json = JsonSerializer.Serialize(requestModel);
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/Profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        return await ReadAuthResponseAsync<CurrentUserViewModel>(response, "Не удалось сохранить изменения профиля.");
    }

    private async Task<AuthApiResult<T>> ReadAuthResponseAsync<T>(HttpResponseMessage response, string fallbackError)
    {
        var result = new AuthApiResult<T>
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode
        };

        var responseJson = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            result.Data = JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
            return result;
        }

        result.ErrorMessage = fallbackError;

        if (string.IsNullOrWhiteSpace(responseJson))
            return result;

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
            {
                result.ErrorMessage = messageElement.GetString() ?? fallbackError;
            }

            if (root.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    var messages = new List<string>();

                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var text = item.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                    messages.Add(text);
                            }
                        }
                    }

                    if (messages.Count > 0)
                    {
                        result.ValidationErrors[property.Name] = messages.ToArray();
                    }
                }
            }
        }
        catch
        {
        }

        return result;
    }
}

public class UpdateProfileRequest
{
    public string Surname { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Theme { get; set; } = "light";
}