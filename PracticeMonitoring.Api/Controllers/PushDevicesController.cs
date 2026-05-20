using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushDevicesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PushNotificationService _pushNotificationService;

    public PushDevicesController(AppDbContext context, PushNotificationService pushNotificationService)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<PushSettingsResponse>> GetSettings()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var hasDevice = await _context.PushDevices.AnyAsync(x => x.UserId == userId.Value && x.IsEnabled);
        return Ok(new PushSettingsResponse
        {
            IsConfigured = _pushNotificationService.IsConfigured,
            HasRegisteredDevice = hasDevice
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(PushDeviceRegistrationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Push-токен устройства не передан." });

        var token = request.Token.Trim();
        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var device = await _context.PushDevices.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (device is null)
        {
            device = new PushDevice
            {
                UserId = userId.Value,
                TokenHash = tokenHash,
                TokenEncrypted = _pushNotificationService.ProtectToken(token),
                Platform = NormalizePlatform(request.Platform),
                DeviceName = NormalizeOptional(request.DeviceName, 200),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                LastSeenAtUtc = now,
                IsEnabled = true
            };
            _context.PushDevices.Add(device);
        }
        else
        {
            device.UserId = userId.Value;
            device.TokenEncrypted = _pushNotificationService.ProtectToken(token);
            device.Platform = NormalizePlatform(request.Platform);
            device.DeviceName = NormalizeOptional(request.DeviceName, 200);
            device.UpdatedAtUtc = now;
            device.LastSeenAtUtc = now;
            device.IsEnabled = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Устройство зарегистрировано для push-уведомлений." });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable(PushDeviceRegistrationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Push-токен устройства не передан." });

        var tokenHash = HashToken(request.Token.Trim());
        await _context.PushDevices
            .Where(x => x.UserId == userId.Value && x.TokenHash == tokenHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsEnabled, false)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));

        return Ok(new { message = "Push-уведомления отключены для устройства." });
    }

    private int? GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizePlatform(string? platform)
    {
        var normalized = string.IsNullOrWhiteSpace(platform) ? "android" : platform.Trim().ToLowerInvariant();
        return normalized.Length <= 40 ? normalized : normalized[..40];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
