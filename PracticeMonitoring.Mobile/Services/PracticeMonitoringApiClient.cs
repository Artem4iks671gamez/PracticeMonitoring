using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class PracticeMonitoringApiClient
{
    private readonly AppSession _session;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PracticeMonitoringApiClient(AppSession session)
    {
        _session = session;
    }

    public Task<ApiResult<object>> SendRegistrationCodeAsync(RegisterRequest request)
        => SendJsonAsync<object>(HttpMethod.Post, "api/auth/send-registration-code", CreateRegistrationPayload(request), "Не удалось отправить код регистрации.");

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
        => SendJsonAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", CreateRegistrationPayload(request, includeCode: true), "Не удалось зарегистрироваться.");

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request)
        => SendJsonAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, "Не удалось войти.");

    public Task<ApiResult<object>> ForgotPasswordAsync(string email)
        => SendJsonAsync<object>(HttpMethod.Post, "api/auth/forgot-password", new { email }, "Не удалось отправить код восстановления.");

    public Task<ApiResult<object>> ResetPasswordAsync(PasswordResetRequest request)
        => SendJsonAsync<object>(HttpMethod.Post, "api/auth/reset-password", new
        {
            request.Email,
            request.Code,
            request.NewPassword
        }, "Не удалось сменить пароль.");

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordRequest request)
        => SendJsonAsync<object>(HttpMethod.Post, "api/auth/change-password", new
        {
            request.CurrentPassword,
            request.NewPassword
        }, "Не удалось сменить пароль.", authorized: true);

    public async Task<CurrentUser?> GetCurrentUserAsync(string? token = null)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, "api/auth/me", token ?? _session.Token);
            using var response = await SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CurrentUser>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public Task<ApiResult<CurrentUser>> UpdateProfileAsync(UpdateProfileRequest request)
        => SendJsonAsync<CurrentUser>(HttpMethod.Put, "api/profile/me", request, "Не удалось сохранить профиль.", authorized: true);

    public Task<List<SpecialtyItem>> GetSpecialtiesAsync()
        => GetListAsync<SpecialtyItem>("api/helping/specialties");

    public Task<List<GroupItem>> GetGroupsAsync(int specialtyId)
        => GetListAsync<GroupItem>($"api/helping/groups?specialtyId={specialtyId}");

    public Task<List<StudentPracticeListItem>> GetPracticesAsync()
        => GetListAsync<StudentPracticeListItem>("api/student/practices", authorized: true);

    public async Task<StudentPracticeDetails?> GetPracticeAsync(int assignmentId)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/student/practices/{assignmentId}", _session.Token);
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StudentPracticeDetails>(json, _jsonOptions);
    }

    public Task<ApiResult<StudentPracticeDetails>> SaveOrganizationAsync(int assignmentId, StudentPracticeOrganizationRequest request)
        => SendJsonAsync<StudentPracticeDetails>(HttpMethod.Put, $"api/student/practices/{assignmentId}/organization", request, "Не удалось сохранить сведения.", authorized: true);

    public Task<ApiResult<StudentPracticeDetails>> SaveDiaryEntryAsync(int assignmentId, StudentPracticeDiaryEntryRequest request)
        => SendJsonAsync<StudentPracticeDetails>(HttpMethod.Put, $"api/student/practices/{assignmentId}/diary", request, "Не удалось сохранить день.", authorized: true);

    public Task<ApiResult<StudentPracticeDetails>> SaveReportItemsAsync(int assignmentId, StudentPracticeReportItemsRequest request)
        => SendJsonAsync<StudentPracticeDetails>(HttpMethod.Put, $"api/student/practices/{assignmentId}/report-items", request, "Не удалось сохранить таблицы.", authorized: true);

    public Task<ApiResult<StudentPracticeDetails>> SaveSourcesAsync(int assignmentId, StudentPracticeSourcesRequest request)
        => SendJsonAsync<StudentPracticeDetails>(HttpMethod.Put, $"api/student/practices/{assignmentId}/sources", request, "Не удалось сохранить источники.", authorized: true);

    public async Task<ApiResult<StudentPracticeAppendixUploadResponse>> UploadAppendixAsync(int assignmentId, string title, string? description, IBrowserFile? file)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/student/practices/{assignmentId}/appendices", _session.Token);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(title ?? string.Empty), "title" },
            { new StringContent(description ?? string.Empty), "description" }
        };

        if (file is not null)
        {
            var streamContent = new StreamContent(file.OpenReadStream(16 * 1024 * 1024));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(streamContent, "file", file.Name);
        }

        request.Content = content;
        return await ReadResultAsync<StudentPracticeAppendixUploadResponse>(await SendAsync(request), "Не удалось загрузить приложение.");
    }

    public Task<ApiResult<object>> DeleteAppendixAsync(int appendixId)
        => SendJsonAsync<object>(HttpMethod.Delete, $"api/student/appendices/{appendixId}", payload: null, "Не удалось удалить приложение.", authorized: true);

    public Task<List<NotificationItem>> GetNotificationsAsync()
        => GetListAsync<NotificationItem>("api/notifications", authorized: true);

    public Task<ApiResult<object>> MarkNotificationReadAsync(int id)
        => SendJsonAsync<object>(HttpMethod.Post, $"api/notifications/{id}/read", payload: null, "Не удалось отметить уведомление.", authorized: true);

    public Task<ApiResult<object>> MarkAllNotificationsReadAsync()
        => SendJsonAsync<object>(HttpMethod.Post, "api/notifications/read-all", payload: null, "Не удалось отметить уведомления.", authorized: true);

    public Task<List<ChatThreadItem>> GetChatThreadsAsync()
        => GetListAsync<ChatThreadItem>("api/chats/threads", authorized: true);

    public Task<List<ChatUser>> SearchContactsAsync(string query)
        => GetListAsync<ChatUser>($"api/chats/contacts/search?query={Uri.EscapeDataString(query ?? string.Empty)}", authorized: true);

    public async Task<ChatThreadDetails?> GetChatThreadAsync(int id)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/chats/threads/{id}", _session.Token);
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChatThreadDetails>(json, _jsonOptions);
    }

    public async Task<ApiResult<ChatMessage>> SendChatMessageAsync(int threadId, int? targetUserId, string? text, IBrowserFile? file)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/chats/threads/{threadId}/messages", _session.Token);
        using var content = new MultipartFormDataContent();
        if (targetUserId.HasValue)
            content.Add(new StringContent(targetUserId.Value.ToString()), "targetUserId");
        content.Add(new StringContent(text ?? string.Empty), "text");

        if (file is not null)
        {
            var streamContent = new StreamContent(file.OpenReadStream(10 * 1024 * 1024));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(streamContent, "attachments", file.Name);
        }

        request.Content = content;
        return await ReadResultAsync<ChatMessage>(await SendAsync(request), "Не удалось отправить сообщение.");
    }

    public async Task<FileDownload?> DownloadAsync(string relativeUrl, string fallbackName)
    {
        using var request = CreateRequest(HttpMethod.Get, relativeUrl, _session.Token);
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return new FileDownload
        {
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                ?? fallbackName
        };
    }

    public async Task SaveAndOpenFileAsync(FileDownload file)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, file.FileName);
        await File.WriteAllBytesAsync(path, file.Content);
        await Launcher.OpenAsync(new OpenFileRequest(file.FileName, new ReadOnlyFile(path)));
    }

    private async Task<List<T>> GetListAsync<T>(string url, bool authorized = false)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, url, authorized ? _session.Token : null);
            using var response = await SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<T>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private async Task<ApiResult<T>> SendJsonAsync<T>(HttpMethod method, string url, object? payload, string fallbackError, bool authorized = false)
    {
        try
        {
            using var request = CreateRequest(method, url, authorized ? _session.Token : null);
            if (payload is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

            return await ReadResultAsync<T>(await SendAsync(request), fallbackError);
        }
        catch (Exception ex)
        {
            return new ApiResult<T>
            {
                Success = false,
                ErrorMessage = $"API недоступно: {ex.Message}"
            };
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(_session.BaseUrl), url));
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static object CreateRegistrationPayload(RegisterRequest request, bool includeCode = false)
    {
        return new
        {
            request.Surname,
            request.Name,
            request.Patronymic,
            request.Email,
            request.Password,
            request.Role,
            request.GroupId,
            Code = includeCode ? request.Code : null
        };
    }

    private static Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
        var client = new HttpClient(handler);
        return client.SendAsync(request);
    }

    private async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, string fallbackError)
    {
        using (response)
        {
            var result = new ApiResult<T>
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            };

            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                result.Data = string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, _jsonOptions);
                return result;
            }

            result.ErrorMessage = fallbackError;
            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    result.ErrorMessage = message.GetString() ?? fallbackError;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errors.EnumerateObject())
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
    }

    private static string? ExtractFileName(string? rawFileName)
        => string.IsNullOrWhiteSpace(rawFileName) ? null : rawFileName.Trim('"');
}
