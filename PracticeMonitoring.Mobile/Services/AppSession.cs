using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class AppSession
{
    private const string TokenKey = "auth_token";
    private const string FullNameKey = "auth_full_name";
    private const string RoleKey = "auth_role";

    public string? Token { get; private set; }
    public string? FullName { get; private set; }
    public string? Role { get; private set; }
    public CurrentUser? CurrentUser { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public async Task LoadAsync()
    {
        Token = await SecureStorage.Default.GetAsync(TokenKey);
        FullName = Preferences.Default.Get(FullNameKey, string.Empty);
        Role = Preferences.Default.Get(RoleKey, string.Empty);
    }

    public async Task SignInAsync(AuthResponse response)
    {
        Token = response.Token;
        FullName = response.FullName;
        Role = response.Role;

        await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        Preferences.Default.Set(FullNameKey, response.FullName);
        Preferences.Default.Set(RoleKey, response.Role);
    }

    public void SignOut()
    {
        Token = null;
        FullName = null;
        Role = null;
        CurrentUser = null;

        SecureStorage.Default.Remove(TokenKey);
        Preferences.Default.Remove(FullNameKey);
        Preferences.Default.Remove(RoleKey);
    }
}
