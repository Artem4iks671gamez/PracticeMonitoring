using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Pages;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile;

public partial class AppShell : Shell
{
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly PushRegistrationService _pushRegistration = ServiceHelper.Get<PushRegistrationService>();
    private ShellContent? _notificationsTab;
    private bool _initialized;
    private bool _studentAreaVisible;

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
        ConfigureAppearance();
        ShowAuthArea();

        Loaded += async (_, _) => await InitializeAsync();
    }

    public async Task SignInToStudentAreaAsync()
    {
        ShowStudentArea();
        await _pushRegistration.EnsureRegisteredAsync();
        await RefreshNotificationBadgeAsync();
        await GoToAsync($"//{nameof(DashboardPage)}");
    }

    public async Task SignOutToLoginAsync()
    {
        if (_session.IsAuthenticated)
        {
            await _pushRegistration.DisableAsync();
            await _api.LogoutAsync(_session.RefreshToken);
        }

        _session.SignOut();
        ShowAuthArea();
        await GoToAsync($"//{nameof(LoginPage)}");
    }

    public void ShowAuthArea()
    {
        if (!_studentAreaVisible && Items.Count == 1 && Items[0].Route == nameof(LoginPage))
            return;

        Items.Clear();
        FlyoutBehavior = FlyoutBehavior.Disabled;
        _studentAreaVisible = false;

        Items.Add(CreateContent<LoginPage>("Вход", nameof(LoginPage)));
    }

    public void ShowStudentArea()
    {
        if (_studentAreaVisible)
            return;

        Items.Clear();
        FlyoutBehavior = FlyoutBehavior.Disabled;
        _studentAreaVisible = true;

        var tabs = new TabBar
        {
            Route = "StudentArea"
        };

        tabs.Items.Add(CreateContent<DashboardPage>("Главная", nameof(DashboardPage)));
        tabs.Items.Add(CreateContent<PracticesPage>("Практики", nameof(PracticesPage)));
        tabs.Items.Add(CreateContent<ChatsPage>("Чаты", nameof(ChatsPage)));
        _notificationsTab = CreateContent<NotificationsPage>("🔔", nameof(NotificationsPage));
        tabs.Items.Add(_notificationsTab);
        tabs.Items.Add(CreateContent<ProfilePage>("Профиль", nameof(ProfilePage)));

        Items.Add(tabs);
    }

    private async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        await _session.LoadAsync();

        if (_session.IsAuthenticated &&
            string.Equals(_session.Role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var me = await _api.GetCurrentUserAsync();
            if (me.Success && string.Equals(me.Data?.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                _session.CurrentUser = me.Data;
                ShowStudentArea();
                await _pushRegistration.EnsureRegisteredAsync();
                await RefreshNotificationBadgeAsync();
                await GoToAsync($"//{nameof(DashboardPage)}");
                return;
            }
        }

        _session.SignOut();
        ShowAuthArea();
    }

    private static ShellContent CreateContent<TPage>(string title, string route)
        where TPage : Page
    {
        return new ShellContent
        {
            Title = title,
            Route = route,
            ContentTemplate = new DataTemplate(typeof(TPage))
        };
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
        Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));
        Routing.RegisterRoute(nameof(PracticeDetailsPage), typeof(PracticeDetailsPage));
        Routing.RegisterRoute(nameof(ChatThreadPage), typeof(ChatThreadPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }

    private void ConfigureAppearance()
    {
        SetBackgroundColor(this, Ui.Surface);
        SetForegroundColor(this, Ui.Text);
        SetTitleColor(this, Ui.Text);
        SetUnselectedColor(this, Ui.Muted);
        SetDisabledColor(this, Ui.Muted);
        SetNavBarHasShadow(this, false);
        SetTabBarBackgroundColor(this, Ui.Surface);
        SetTabBarForegroundColor(this, Ui.PrimarySoft);
        SetTabBarTitleColor(this, Ui.PrimarySoft);
        SetTabBarUnselectedColor(this, Ui.Muted);
    }

    public async Task RefreshNotificationBadgeAsync()
    {
        if (_notificationsTab is null || !_studentAreaVisible || string.IsNullOrWhiteSpace(_session.Token))
            return;

        var result = await _api.GetNotificationsAsync();
        if (!result.Success || result.Data is null)
        {
            _notificationsTab.Title = "🔔";
            return;
        }

        var unread = result.Data.Count(x => !x.IsRead);
        _notificationsTab.Title = unread > 0 ? $"🔔 {Math.Min(unread, 99)}" : "🔔";
    }
}
