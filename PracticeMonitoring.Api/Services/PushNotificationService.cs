using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public sealed class PushNotificationService
{
    private const string FirebaseMessagingScope = "https://www.googleapis.com/auth/firebase.messaging";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDataProtector _protector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IServiceScopeFactory scopeFactory,
        IDataProtectionProvider dataProtectionProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PushNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _protector = dataProtectionProvider.CreateProtector("PracticeMonitoring.PushDeviceTokens.v1");
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetProjectId()) && !string.IsNullOrWhiteSpace(GetServiceAccountJson());

    public string ProtectToken(string token) => _protector.Protect(token.Trim());

    public string UnprotectToken(string protectedToken) => _protector.Unprotect(protectedToken);

    public async Task SendToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || !IsConfigured)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var devices = await context.PushDevices
                .Where(x => x.UserId == userId && x.IsEnabled)
                .ToListAsync(cancellationToken);

            if (devices.Count == 0)
                return;

            var projectId = GetProjectId()!;
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            using var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            foreach (var device in devices)
            {
                var token = UnprotectToken(device.TokenEncrypted);
                var payload = BuildPayload(token, title, body, data);
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var response = await http.PostAsync($"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                    continue;

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("FCM send failed for device {DeviceId}: {StatusCode} {Response}", device.Id, response.StatusCode, responseText);

                if ((int)response.StatusCode is 400 or 404)
                    device.IsEnabled = false;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send push notification to user {UserId}.", userId);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credential = GoogleCredential
            .FromJson(GetServiceAccountJson())
            .CreateScoped(FirebaseMessagingScope)
            .UnderlyingCredential;

        return await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }

    private string? GetProjectId() => _configuration["Firebase:ProjectId"];

    private string? GetServiceAccountJson()
    {
        var json = _configuration["Firebase:ServiceAccountJson"];
        if (!string.IsNullOrWhiteSpace(json))
            return json;

        var path = _configuration["Firebase:ServiceAccountPath"];
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? File.ReadAllText(path)
            : null;
    }

    private static object BuildPayload(string token, string title, string body, Dictionary<string, string>? data)
    {
        return new
        {
            message = new
            {
                token,
                notification = new
                {
                    title,
                    body
                },
                data = data ?? new Dictionary<string, string>(),
                android = new
                {
                    priority = "HIGH",
                    notification = new
                    {
                        channel_id = "practice_monitoring_updates"
                    }
                }
            }
        };
    }
}
