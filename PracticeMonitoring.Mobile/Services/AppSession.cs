using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class AppSession
{
    private const string TokenKey = "practice_monitoring_token";

    public string Token { get; private set; } = string.Empty;
    public CurrentUser? User { get; private set; }
    public string BaseUrl { get; private set; } = DefaultBaseUrl;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && User is not null;
    public event Action? Changed;

    public static string DefaultBaseUrl
    {
        get
        {
#if ANDROID
            return "https://10.0.2.2:7178/";
#else
            return "https://localhost:7178/";
#endif
        }
    }

    public async Task InitializeAsync(PracticeMonitoringApiClient apiClient)
    {
        BaseUrl = DefaultBaseUrl;

        try
        {
            Token = await SecureStorage.GetAsync(TokenKey) ?? string.Empty;
        }
        catch
        {
            Token = Preferences.Get(TokenKey, string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(Token))
        {
            try
            {
                User = await apiClient.GetCurrentUserAsync(Token);
            }
            catch
            {
                User = null;
            }

            if (User?.Role != "Student")
                await ClearAsync();
        }

        Notify();
    }

    public async Task SignInAsync(string token, CurrentUser user)
    {
        Token = token;
        User = user;

        try
        {
            await SecureStorage.SetAsync(TokenKey, token);
        }
        catch
        {
            Preferences.Set(TokenKey, token);
        }

        Notify();
    }

    public void SetUser(CurrentUser user)
    {
        User = user;
        Notify();
    }

    public Task ClearAsync()
    {
        Token = string.Empty;
        User = null;
        SecureStorage.Remove(TokenKey);
        Preferences.Remove(TokenKey);
        Notify();
        return Task.CompletedTask;
    }

    private void Notify() => Changed?.Invoke();
}
