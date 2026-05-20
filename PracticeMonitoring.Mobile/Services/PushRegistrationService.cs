using PracticeMonitoring.Mobile.Models;

#if ANDROID
using Firebase.Messaging;
using Android.OS;
#endif

namespace PracticeMonitoring.Mobile.Services;

public sealed class PushRegistrationService
{
    public const string DeviceTokenPreferenceKey = "push_device_token";
    private const string EnabledPreferenceKey = "push_enabled";
    private readonly ApiClient _api;
    private readonly AppSession _session;

    public PushRegistrationService(ApiClient api, AppSession session)
    {
        _api = api;
        _session = session;
    }

    public bool IsEnabled
    {
        get => Preferences.Default.Get(EnabledPreferenceKey, true);
        set => Preferences.Default.Set(EnabledPreferenceKey, value);
    }

    public async Task<ApiResult<PushSettings>> GetSettingsAsync()
    {
        return await _api.GetPushSettingsAsync();
    }

    public async Task<bool> EnsureRegisteredAsync(bool askPermission = true)
    {
        if (!_session.IsAuthenticated || !IsEnabled)
            return false;

#if ANDROID
        if (askPermission && !await EnsurePermissionAsync())
        {
            IsEnabled = false;
            return false;
        }

        var token = await GetDeviceTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return false;

        Preferences.Default.Set(DeviceTokenPreferenceKey, token);
        var result = await _api.RegisterPushDeviceAsync(token, "android", DeviceInfo.Current.Name);
        return result.Success;
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    public async Task<bool> DisableAsync()
    {
        IsEnabled = false;
        var token = Preferences.Default.Get(DeviceTokenPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(token) || !_session.IsAuthenticated)
            return true;

        var result = await _api.DisablePushDeviceAsync(token, "android", DeviceInfo.Current.Name);
        return result.Success;
    }

#if ANDROID
    private static async Task<bool> EnsurePermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return true;

        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted)
            return true;

        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
    }

    private static Task<string?> GetDeviceTokenAsync()
    {
        var completion = new TaskCompletionSource<string?>();
        try
        {
            FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(new TokenCompleteListener(completion));
        }
        catch
        {
            completion.TrySetResult(null);
        }

        return completion.Task;
    }

    private sealed class TokenCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        private readonly TaskCompletionSource<string?> _completion;

        public TokenCompleteListener(TaskCompletionSource<string?> completion)
        {
            _completion = completion;
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (!task.IsSuccessful)
            {
                _completion.TrySetResult(null);
                return;
            }

            _completion.TrySetResult(task.Result?.ToString());
        }
    }
#endif
}
