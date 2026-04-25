using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PracticeMonitoring.Web.Models.Messaging;

namespace PracticeMonitoring.Web.Services;

public class ChatApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ChatApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ChatThreadListItemViewModel>> GetThreadsAsync(string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/chats/threads", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<ChatThreadListItemViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ChatThreadListItemViewModel>>(json, _jsonOptions)
               ?? new List<ChatThreadListItemViewModel>();
    }

    public async Task<List<ChatUserShortViewModel>> SearchContactsAsync(string token, string? query)
    {
        var suffix = string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : $"?query={Uri.EscapeDataString(query.Trim())}";

        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/chats/contacts/search{suffix}", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<ChatUserShortViewModel>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ChatUserShortViewModel>>(json, _jsonOptions)
               ?? new List<ChatUserShortViewModel>();
    }

    public async Task<ChatThreadDetailsViewModel?> GetThreadAsync(string token, int threadId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/chats/threads/{threadId}", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChatThreadDetailsViewModel>(json, _jsonOptions);
    }

    public async Task<ChatApiResult<ChatThreadListItemViewModel>> StartThreadAsync(string token, int targetUserId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "api/chats/threads", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { targetUserId }),
            Encoding.UTF8,
            "application/json");

        return await SendAsync<ChatThreadListItemViewModel>(request, "Не удалось открыть диалог.");
    }

    public async Task<ChatApiResult<ChatMessageViewModel>> SendMessageAsync(
        string token,
        int threadId,
        int? targetUserId,
        string? text,
        IEnumerable<IFormFile>? attachments)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/chats/threads/{threadId}/messages", token);
        using var formData = new MultipartFormDataContent();

        if (targetUserId.HasValue)
            formData.Add(new StringContent(targetUserId.Value.ToString()), "targetUserId");

        if (!string.IsNullOrWhiteSpace(text))
            formData.Add(new StringContent(text), "text");

        if (attachments is not null)
        {
            foreach (var attachment in attachments.Where(x => x.Length > 0))
            {
                var streamContent = new StreamContent(attachment.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType);

                formData.Add(streamContent, "attachments", attachment.FileName);
            }
        }

        request.Content = formData;
        return await SendAsync<ChatMessageViewModel>(request, "Не удалось отправить сообщение.");
    }

    public async Task<ChatFileResult?> DownloadAttachmentAsync(string token, int attachmentId)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/chats/attachments/{attachmentId}/download", token);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return new ChatFileResult
        {
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = ExtractFileName(response.Content.Headers.ContentDisposition?.FileNameStar)
                       ?? ExtractFileName(response.Content.Headers.ContentDisposition?.FileName)
                       ?? "attachment.bin"
        };
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<ChatApiResult<T>> SendAsync<T>(HttpRequestMessage request, string fallbackMessage)
    {
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        var result = new ChatApiResult<T>
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
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("message", out var messageElement))
                result.ErrorMessage = messageElement.GetString() ?? fallbackMessage;
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
