using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class ApiClient
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string WebBaseUrlKey = "web_base_url";
#if ANDROID
    private const string DefaultApiBaseUrl = "https://10.0.2.2:7178/";
    private const string DefaultWebBaseUrl = "https://10.0.2.2:7128/";
#else
    private const string DefaultApiBaseUrl = "https://localhost:7178/";
    private const string DefaultWebBaseUrl = "https://localhost:7128/";
#endif
    private readonly AppSession _session;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(AppSession session)
    {
        _session = session;
    }

    public string ApiBaseUrl
    {
        get => NormalizeBaseUrl(FixLocalhostForAndroid(Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl), DefaultApiBaseUrl));
        set => Preferences.Default.Set(ApiBaseUrlKey, NormalizeBaseUrl(value));
    }

    public string WebBaseUrl
    {
        get => NormalizeBaseUrl(FixLocalhostForAndroid(Preferences.Default.Get(WebBaseUrlKey, DefaultWebBaseUrl), DefaultWebBaseUrl));
        set => Preferences.Default.Set(WebBaseUrlKey, NormalizeBaseUrl(value));
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(string email, string password)
    {
        return PostJsonAsync<AuthResponse>("api/Auth/login", new { Email = email, Password = password }, requiresAuth: false);
    }

    public Task<ApiResult<object>> SendRegistrationCodeAsync(RegisterRequest request)
    {
        return PostJsonAsync<object>("api/Auth/send-registration-code", request, requiresAuth: false);
    }

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        return PostJsonAsync<AuthResponse>("api/Auth/register", request, requiresAuth: false);
    }

    public Task<ApiResult<object>> ForgotPasswordAsync(string email)
    {
        return PostJsonAsync<object>("api/Auth/forgot-password", new { Email = email }, requiresAuth: false);
    }

    public Task<ApiResult<object>> ResetPasswordAsync(string email, string code, string newPassword)
    {
        return PostJsonAsync<object>("api/Auth/reset-password", new { Email = email, Code = code, NewPassword = newPassword }, requiresAuth: false);
    }

    public Task<ApiResult<object>> ChangePasswordAsync(string? currentPassword, string newPassword)
    {
        return PostJsonAsync<object>("api/Auth/change-password", new { CurrentPassword = currentPassword, NewPassword = newPassword });
    }

    public Task<ApiResult<CurrentUser>> GetCurrentUserAsync()
    {
        return GetAsync<CurrentUser>("api/Auth/me");
    }

    public Task<ApiResult<CurrentUser>> UpdateProfileAsync(CurrentUser user)
    {
        return PutJsonAsync<CurrentUser>("api/Profile/me", new
        {
            user.Surname,
            user.FirstName,
            user.Patronymic,
            user.Email,
            user.AvatarUrl,
            user.Theme
        });
    }

    public Task<ApiResult<List<SpecialtyOption>>> GetSpecialtiesAsync()
    {
        return GetAsync<List<SpecialtyOption>>("api/Helping/specialties", requiresAuth: false);
    }

    public Task<ApiResult<List<GroupOption>>> GetGroupsAsync(int specialtyId)
    {
        return GetAsync<List<GroupOption>>($"api/Helping/groups?specialtyId={specialtyId}", requiresAuth: false);
    }

    public Task<ApiResult<List<PracticeListItem>>> GetPracticesAsync()
    {
        return GetAsync<List<PracticeListItem>>("api/Student/practices");
    }

    public Task<ApiResult<PracticeDetails>> GetPracticeAsync(int assignmentId)
    {
        return GetAsync<PracticeDetails>($"api/Student/practices/{assignmentId}");
    }

    public Task<ApiResult<PracticeDetails>> SaveOrganizationAsync(int assignmentId, PracticeDetails practice)
    {
        return PutJsonAsync<PracticeDetails>($"api/Student/practices/{assignmentId}/organization", new
        {
            practice.OrganizationName,
            practice.OrganizationFullName,
            practice.OrganizationShortName,
            practice.OrganizationAddress,
            practice.OrganizationSupervisorFullName,
            practice.OrganizationSupervisorPosition,
            practice.OrganizationSupervisorPhone,
            practice.OrganizationSupervisorEmail,
            practice.PracticeTaskContent,
            practice.StudentDuties,
            practice.ProvidedMaterialsDescription,
            practice.WorkScheduleDescription,
            practice.IntroductionMainGoal
        });
    }

    public Task<ApiResult<PracticeDetails>> SaveDiaryEntryAsync(int assignmentId, DiaryEntry entry, IEnumerable<int>? keptAttachmentIds = null)
    {
        return PutJsonAsync<PracticeDetails>($"api/Student/practices/{assignmentId}/diary", new
        {
            entry.WorkDate,
            entry.ShortDescription,
            entry.DetailedReport,
            Figures = Array.Empty<object>(),
            KeptAttachmentIds = keptAttachmentIds?.ToArray() ?? entry.Attachments.Select(x => x.Id).ToArray()
        });
    }

    public Task<ApiResult<PracticeDetails>> SaveReportItemsAsync(int assignmentId, IEnumerable<ReportItem> items)
    {
        return PutJsonAsync<PracticeDetails>($"api/Student/practices/{assignmentId}/report-items", new
        {
            Items = items.Select(x => new { x.Category, x.Name, x.Description }).ToList()
        });
    }

    public Task<ApiResult<PracticeDetails>> SaveSourcesAsync(int assignmentId, IEnumerable<PracticeSource> sources)
    {
        return PutJsonAsync<PracticeDetails>($"api/Student/practices/{assignmentId}/sources", new
        {
            Sources = sources.Select(x => new { x.Title, x.Url, x.Description }).ToList()
        });
    }

    public Task<ApiResult<object>> DeleteAppendixAsync(int appendixId)
    {
        return DeleteAsync<object>($"api/Student/appendices/{appendixId}");
    }

    public async Task<ApiResult<PracticeDetails>> UploadAppendixAsync(int assignmentId, FileResult file, string? title, string? description)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(title ?? string.Empty), "title");
        form.Add(new StringContent(description ?? string.Empty), "description");
        await using var stream = await file.OpenReadAsync();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(fileContent, "file", file.FileName);

        return await SendFormAsync<PracticeDetails>($"api/Student/practices/{assignmentId}/appendices", form);
    }

    public async Task<ApiResult<object>> UploadDiaryAttachmentAsync(int assignmentId, DateTime workDate, string? title, FileResult file)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(workDate.ToString("O")), "workDate");
        form.Add(new StringContent(title ?? string.Empty), "title");
        await using var stream = await file.OpenReadAsync();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(fileContent, "file", file.FileName);

        return await SendFormAsync<object>($"api/Student/practices/{assignmentId}/diary-attachments", form);
    }

    public Task<ApiResult<List<NotificationItem>>> GetNotificationsAsync()
    {
        return GetAsync<List<NotificationItem>>("api/Notifications");
    }

    public Task<ApiResult<object>> MarkNotificationReadAsync(int id)
    {
        return PostJsonAsync<object>($"api/Notifications/{id}/read", new { });
    }

    public Task<ApiResult<object>> MarkAllNotificationsReadAsync()
    {
        return PostJsonAsync<object>("api/Notifications/read-all", new { });
    }

    public Task<ApiResult<List<ChatThreadItem>>> GetThreadsAsync()
    {
        return GetAsync<List<ChatThreadItem>>("api/Chats/threads");
    }

    public Task<ApiResult<ChatThreadDetails>> GetThreadAsync(int id)
    {
        return GetAsync<ChatThreadDetails>($"api/Chats/threads/{id}");
    }

    public Task<ApiResult<List<ChatUser>>> SearchContactsAsync(string query)
    {
        return GetAsync<List<ChatUser>>($"api/Chats/contacts/search?query={Uri.EscapeDataString(query)}");
    }

    public Task<ApiResult<ChatThreadDetails>> StartThreadAsync(int targetUserId)
    {
        return PostJsonAsync<ChatThreadDetails>("api/Chats/threads", new { TargetUserId = targetUserId });
    }

    public async Task<ApiResult<ChatMessage>> SendMessageAsync(int threadId, string text, FileResult? attachment = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(text ?? string.Empty), "text");
        if (attachment is not null)
        {
            await using var stream = await attachment.OpenReadAsync();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
            form.Add(fileContent, "attachments", attachment.FileName);
        }

        return await SendFormAsync<ChatMessage>($"api/Chats/threads/{threadId}/messages", form);
    }

    public Task<ApiResult<FileDownload>> DownloadApiFileAsync(string relativeUrl)
    {
        return DownloadFileAsync(ApiBaseUrl, relativeUrl);
    }

    public Task<ApiResult<FileDownload>> DownloadWebDocumentAsync(string relativeUrl)
    {
        return DownloadFileAsync(WebBaseUrl, relativeUrl);
    }

    private Task<ApiResult<T>> GetAsync<T>(string relativeUrl, bool requiresAuth = true)
    {
        return SendAsync<T>(HttpMethod.Get, relativeUrl, null, requiresAuth);
    }

    private Task<ApiResult<T>> PostJsonAsync<T>(string relativeUrl, object payload, bool requiresAuth = true)
    {
        return SendAsync<T>(HttpMethod.Post, relativeUrl, JsonContent(payload), requiresAuth);
    }

    private Task<ApiResult<T>> PutJsonAsync<T>(string relativeUrl, object payload)
    {
        return SendAsync<T>(HttpMethod.Put, relativeUrl, JsonContent(payload), requiresAuth: true);
    }

    private Task<ApiResult<T>> DeleteAsync<T>(string relativeUrl)
    {
        return SendAsync<T>(HttpMethod.Delete, relativeUrl, null, requiresAuth: true);
    }

    private async Task<ApiResult<T>> SendFormAsync<T>(string relativeUrl, HttpContent form)
    {
        using var request = CreateRequest(HttpMethod.Post, ApiBaseUrl, relativeUrl, requiresAuth: true);
        request.Content = form;
        return await SendRequestAsync<T>(request);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string relativeUrl, HttpContent? content, bool requiresAuth)
    {
        using var request = CreateRequest(method, ApiBaseUrl, relativeUrl, requiresAuth);
        request.Content = content;
        return await SendRequestAsync<T>(request);
    }

    private async Task<ApiResult<T>> SendRequestAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var http = CreateHttpClient();
            using var response = await http.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseText)
                    ? default
                    : JsonSerializer.Deserialize<T>(responseText, _jsonOptions);

                return new ApiResult<T>
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Data = data
                };
            }

            return new ApiResult<T>
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = ExtractError(responseText) ?? $"HTTP {(int)response.StatusCode}",
                ValidationErrors = ExtractValidationErrors(responseText)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 0,
                ErrorMessage = BuildConnectionError(request.RequestUri, ex)
            };
        }
    }

    private async Task<ApiResult<FileDownload>> DownloadFileAsync(string baseUrl, string relativeUrl)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, relativeUrl, requiresAuth: true);
            using var http = CreateHttpClient();
            using var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                return new ApiResult<FileDownload>
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = ExtractError(text) ?? $"HTTP {(int)response.StatusCode}"
                };
            }

            return new ApiResult<FileDownload>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = new FileDownload
                {
                    Content = await response.Content.ReadAsByteArrayAsync(),
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
                    FileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                               ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                               ?? Path.GetFileName(relativeUrl.Split('?')[0])
                               ?? "document.bin"
                }
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new ApiResult<FileDownload>
            {
                Success = false,
                ErrorMessage = BuildConnectionError(new Uri(new Uri(NormalizeBaseUrl(baseUrl)), relativeUrl.TrimStart('/')), ex)
            };
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string baseUrl, string relativeUrl, bool requiresAuth)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(NormalizeBaseUrl(baseUrl)), relativeUrl.TrimStart('/')));
        if (requiresAuth && !string.IsNullOrWhiteSpace(_session.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);

        return request;
    }

    private static HttpClient CreateHttpClient()
    {
#if DEBUG
        return new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
#else
        return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
#endif
    }

    private static StringContent JsonContent(object payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static string NormalizeBaseUrl(string? value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? DefaultApiBaseUrl : value.Trim();
        return result.EndsWith("/", StringComparison.Ordinal) ? result : $"{result}/";
    }

    private static string FixLocalhostForAndroid(string? value, string androidDefault)
    {
#if ANDROID
        if (string.IsNullOrWhiteSpace(value))
            return androidDefault;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return androidDefault;
            }

            if (uri.Host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase) &&
                uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                (uri.Port is 5149 or 5242))
            {
                return androidDefault;
            }
        }
#endif
        return value ?? androidDefault;
    }

    private static string BuildConnectionError(Uri? uri, Exception exception)
    {
        var target = uri is null ? "API" : uri.GetLeftPart(UriPartial.Authority);
        return $"Не удалось подключиться к {target}. Для Android-эмулятора локальный API должен быть https://10.0.2.2:7178/. Техническая причина: {exception.Message}";
    }

    private static string? ExtractError(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch
        {
        }

        return json.Length <= 500 ? json : json[..500];
    }

    private static Dictionary<string, string[]> ExtractValidationErrors(string json)
    {
        var result = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("errors", out var errors) ||
                errors.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var property in errors.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;

                result[property.Name] = property.Value
                    .EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }
        }
        catch
        {
        }

        return result;
    }

    private static string? ExtractFileName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim('"');
    }
}
