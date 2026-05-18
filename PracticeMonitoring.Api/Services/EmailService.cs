using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PracticeMonitoring.Api.Services;

public class EmailService
{
    private readonly SmtpOptions _options;
    private readonly UnisenderOptions _unisenderOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        IOptions<SmtpOptions> options,
        IOptions<UnisenderOptions> unisenderOptions,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _unisenderOptions = unisenderOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        if (!string.IsNullOrWhiteSpace(_unisenderOptions.ApiKey))
        {
            await SendViaUnisenderAsync(toEmail, subject, htmlBody, cancellationToken);
            return;
        }

        ValidateOptions();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject.Trim();
        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task SendViaUnisenderAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var fromEmail = string.IsNullOrWhiteSpace(_unisenderOptions.FromEmail)
            ? _options.FromEmail
            : _unisenderOptions.FromEmail;

        var fromName = string.IsNullOrWhiteSpace(_unisenderOptions.FromName)
            ? _options.FromName
            : _unisenderOptions.FromName;

        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("Unisender:FromEmail or Smtp:FromEmail is not configured.");

        var listId = await GetUnisenderListIdAsync(cancellationToken);
        var responseBody = await PostUnisenderFormAsync(
            "sendEmail",
            new Dictionary<string, string>
            {
                ["email"] = toEmail,
                ["sender_name"] = fromName,
                ["sender_email"] = fromEmail,
                ["subject"] = subject.Trim(),
                ["body"] = htmlBody,
                ["list_id"] = listId,
                ["lang"] = "ru",
                ["track_read"] = "0",
                ["track_links"] = "0",
                ["error_checking"] = "1"
            },
            cancellationToken);

        var sendError = GetUnisenderSendError(responseBody);
        if (sendError is not null)
            throw new InvalidOperationException($"Unisender email API failed: {sendError}");
    }

    private async Task<string> GetUnisenderListIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_unisenderOptions.ListId))
            return _unisenderOptions.ListId;

        var listTitle = string.IsNullOrWhiteSpace(_unisenderOptions.ListTitle)
            ? "Practice Monitoring"
            : _unisenderOptions.ListTitle;

        var listsResponse = await PostUnisenderFormAsync("getLists", new Dictionary<string, string>(), cancellationToken);
        var existingListId = FindUnisenderListId(listsResponse, listTitle);
        if (existingListId is not null)
            return existingListId;

        var createResponse = await PostUnisenderFormAsync(
            "createList",
            new Dictionary<string, string>
            {
                ["title"] = listTitle
            },
            cancellationToken);

        return GetCreatedUnisenderListId(createResponse)
            ?? throw new InvalidOperationException("Unisender API did not return a created list id.");
    }

    private async Task<string> PostUnisenderFormAsync(string method, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        values["format"] = "json";
        values["api_key"] = _unisenderOptions.ApiKey;

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUnisenderUrl(method));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(values);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Unisender email API failed with status {(int)response.StatusCode}: {responseBody}");

        var apiError = GetUnisenderTopLevelError(responseBody);
        if (apiError is not null)
            throw new InvalidOperationException($"Unisender email API failed: {apiError}");

        return responseBody;
    }

    private string BuildUnisenderUrl(string method)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_unisenderOptions.BaseUrl)
            ? "https://api.unisender.com/ru/api"
            : _unisenderOptions.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/{method}";
    }

    private static string? FindUnisenderListId(string responseBody, string listTitle)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in result.EnumerateArray())
        {
            if (!item.TryGetProperty("title", out var title) ||
                !string.Equals(title.GetString(), listTitle, StringComparison.OrdinalIgnoreCase))
                continue;

            return item.TryGetProperty("id", out var id)
                ? GetJsonScalarValue(id)
                : null;
        }

        return null;
    }

    private static string? GetCreatedUnisenderListId(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            return null;

        return result.TryGetProperty("id", out var id)
            ? GetJsonScalarValue(id)
            : null;
    }

    private static string? GetUnisenderTopLevelError(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out var error))
                return null;

            var message = error.GetString();
            if (document.RootElement.TryGetProperty("code", out var code))
                return $"{GetJsonScalarValue(code)}: {message}";

            return message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetUnisenderSendError(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return null;

        var errors = new List<string>();
        foreach (var item in result.EnumerateArray())
        {
            if (!item.TryGetProperty("errors", out var itemErrors) || itemErrors.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var error in itemErrors.EnumerateArray())
            {
                var code = error.TryGetProperty("code", out var codeValue)
                    ? GetJsonScalarValue(codeValue)
                    : "unknown";

                var message = error.TryGetProperty("message", out var messageValue)
                    ? messageValue.GetString()
                    : "Unknown error";

                errors.Add($"{code}: {message}");
            }
        }

        return errors.Count == 0 ? null : string.Join("; ", errors);
    }

    private static string? GetJsonScalarValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Smtp:Host is not configured.");

        if (_options.Port <= 0)
            throw new InvalidOperationException("Smtp:Port is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("Smtp:Username is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("Smtp:Password is not configured.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Smtp:FromEmail is not configured.");
    }
}
