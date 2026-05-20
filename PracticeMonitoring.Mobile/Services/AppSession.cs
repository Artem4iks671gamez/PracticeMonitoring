using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class AppSession
{
    private const string TokenKey = "auth_token";
    private const string TokenExpiresAtKey = "auth_token_expires_at";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string RefreshTokenExpiresAtKey = "auth_refresh_token_expires_at";
    private const string FullNameKey = "auth_full_name";
    private const string RoleKey = "auth_role";

    public string? Token { get; private set; }
    public DateTime? TokenExpiresAtUtc { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; private set; }
    public string? FullName { get; private set; }
    public string? Role { get; private set; }
    public CurrentUser? CurrentUser { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public async Task LoadAsync()
    {
        Token = await SecureStorage.Default.GetAsync(TokenKey);
        RefreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        TokenExpiresAtUtc = ReadDate(TokenExpiresAtKey);
        RefreshTokenExpiresAtUtc = ReadDate(RefreshTokenExpiresAtKey);
        FullName = Preferences.Default.Get(FullNameKey, string.Empty);
        Role = Preferences.Default.Get(RoleKey, string.Empty);
    }

    public async Task SignInAsync(AuthResponse response)
    {
        Token = response.Token;
        TokenExpiresAtUtc = response.TokenExpiresAtUtc;
        RefreshToken = response.RefreshToken;
        RefreshTokenExpiresAtUtc = response.RefreshTokenExpiresAtUtc;
        FullName = response.FullName;
        Role = response.Role;

        await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            await SecureStorage.Default.SetAsync(RefreshTokenKey, response.RefreshToken);

        Preferences.Default.Set(TokenExpiresAtKey, response.TokenExpiresAtUtc.ToString("O"));
        Preferences.Default.Set(RefreshTokenExpiresAtKey, response.RefreshTokenExpiresAtUtc.ToString("O"));
        Preferences.Default.Set(FullNameKey, response.FullName);
        Preferences.Default.Set(RoleKey, response.Role);
    }

    public void SignOut()
    {
        Token = null;
        TokenExpiresAtUtc = null;
        RefreshToken = null;
        RefreshTokenExpiresAtUtc = null;
        FullName = null;
        Role = null;
        CurrentUser = null;

        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        Preferences.Default.Remove(TokenExpiresAtKey);
        Preferences.Default.Remove(RefreshTokenExpiresAtKey);
        Preferences.Default.Remove(FullNameKey);
        Preferences.Default.Remove(RoleKey);
    }

    private static DateTime? ReadDate(string key)
    {
        var value = Preferences.Default.Get(key, string.Empty);
        return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;
    }
}
