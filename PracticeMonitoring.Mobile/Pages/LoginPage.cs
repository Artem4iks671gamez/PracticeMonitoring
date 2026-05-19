using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class LoginPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly Entry _email = Ui.Entry("Email", Keyboard.Email);
    private readonly Entry _password = Ui.Entry("Пароль", isPassword: true);
    private readonly Label _error = ErrorLabel();

    protected override bool AllowAnonymous => true;

    public LoginPage()
    {
        Title = "Вход";
        Shell.SetNavBarIsVisible(this, false);

        var loginButton = Ui.PrimaryButton("Войти");
        loginButton.Clicked += async (_, _) => await LoginAsync();

        var registerButton = Ui.SecondaryButton("Создать аккаунт");
        registerButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(RegisterPage));

        var forgotButton = Ui.GhostButton("Забыли пароль");
        forgotButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ResetPasswordPage));

        var settingsButton = Ui.GhostButton("Настройки подключения");
        settingsButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(SettingsPage));

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Ui.Hero("Practice Monitoring", "Мобильный кабинет студента"),
                Ui.Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        Ui.Eyebrow("Авторизация"),
                        Ui.Title("Вход в приложение", 22),
                        Ui.Caption("После входа откроются практики, дневник, документы, чаты и уведомления."),
                        _email,
                        _password,
                        _error,
                        loginButton,
                        registerButton,
                        Ui.ActionRow(forgotButton, settingsButton)
                    }
                })
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _session.LoadAsync();
        if (!_session.IsAuthenticated)
            return;

        var me = await _api.GetCurrentUserAsync();
        if (me.Success && me.Data?.Role == "Student")
        {
            _session.CurrentUser = me.Data;
            if (Shell.Current is AppShell shell)
                await shell.SignInToStudentAreaAsync();
        }
        else
        {
            _session.SignOut();
        }
    }

    private async Task LoginAsync()
    {
        ShowError(_error, string.Empty);
        await RunBusyAsync(async () =>
        {
            var result = await _api.LoginAsync(_email.Text ?? string.Empty, _password.Text ?? string.Empty);
            if (!result.Success || result.Data is null)
            {
                ShowError(_error, ErrorText(result, "Не удалось войти."));
                return;
            }

            if (!string.Equals(result.Data.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                ShowError(_error, "Мобильное приложение предназначено только для роли Student.");
                return;
            }

            await _session.SignInAsync(result.Data);
            if (result.Data.MustChangePassword)
            {
                await Shell.Current.GoToAsync(nameof(ChangePasswordPage));
                return;
            }

            if (Shell.Current is AppShell shell)
                await shell.SignInToStudentAreaAsync();
        });
    }
}
