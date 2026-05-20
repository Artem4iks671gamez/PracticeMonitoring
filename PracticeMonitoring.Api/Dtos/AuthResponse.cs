namespace PracticeMonitoring.Api.Dtos;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime TokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool MustChangePassword { get; set; }
}

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
