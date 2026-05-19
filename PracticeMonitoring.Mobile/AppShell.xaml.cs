using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Pages;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile;

public partial class AppShell : Shell
{
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
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
        await GoToAsync($"//{nameof(DashboardPage)}");
    }

    public async Task SignOutToLoginAsync()
    {
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
        tabs.Items.Add(CreateContent<NotificationsPage>("Уведомл.", nameof(NotificationsPage)));
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
}
