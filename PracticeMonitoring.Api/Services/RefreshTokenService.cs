using System.Security.Cryptography;
using System.Text;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public sealed class RefreshTokenService
{
    private readonly IConfiguration _configuration;

    public RefreshTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IssuedRefreshToken Create(User user, HttpContext httpContext, string? replacedByTokenHash = null)
    {
        var rawToken = GenerateRawToken();
        var now = DateTime.UtcNow;
        var expiresDays = _configuration.GetValue<int?>("Jwt:RefreshTokenExpiresDays") ?? 30;

        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Math.Clamp(expiresDays, 1, 180)),
            ReplacedByTokenHash = replacedByTokenHash,
            DeviceName = Trim(httpContext.Request.Headers.UserAgent.ToString(), 200),
            UserAgent = Trim(httpContext.Request.Headers.UserAgent.ToString(), 500),
            IpAddress = Trim(httpContext.Connection.RemoteIpAddress?.ToString(), 80)
        };

        return new IssuedRefreshToken(entity, rawToken);
    }

    public string HashToken(string token) => Hash(token);

    private static string GenerateRawToken()
    {
        return Base64Url(RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public sealed record IssuedRefreshToken(RefreshToken Entity, string RawToken);
