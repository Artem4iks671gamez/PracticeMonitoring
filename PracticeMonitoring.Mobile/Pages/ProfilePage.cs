using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ProfilePage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly Entry _surname = Ui.Entry("Фамилия");
    private readonly Entry _name = Ui.Entry("Имя");
    private readonly Entry _patronymic = Ui.Entry("Отчество");
    private readonly Entry _email = Ui.Entry("Email", Keyboard.Email);
    private readonly Entry _avatar = Ui.Entry("URL аватарки");
    private readonly Label _info = ErrorLabel();
    private readonly VerticalStackLayout _root = new() { Spacing = 14 };
    private CurrentUser? _user;

    public ProfilePage()
    {
        Title = "Профиль";
        Content = Scroll(_root);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await RequireStudentAsync())
            return;

        RenderSkeleton();
        await LoadAsync();
    }

    private void RenderSkeleton()
    {
        _root.Children.Clear();
        _root.Children.Add(Ui.Hero("Профиль", _session.FullName ?? "Студент"));
        _root.Children.Add(Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Eyebrow("Данные аккаунта"),
                _surname,
                _name,
                _patronymic,
                _email,
                _avatar,
                _info,
                CreateActions()
            }
        }));
    }

    private View CreateActions()
    {
        var save = Ui.PrimaryButton("Сохранить");
        save.Clicked += async (_, _) => await SaveAsync();

        var password = Ui.SecondaryButton("Пароль");
        password.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ChangePasswordPage));

        var settings = Ui.SecondaryButton("API");
        settings.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(SettingsPage));

        var logout = Ui.SecondaryButton("Выйти");
        logout.TextColor = Ui.Danger;
        logout.Clicked += async (_, _) =>
        {
            if (Shell.Current is AppShell shell)
                await shell.SignOutToLoginAsync();
        };

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                save,
                Ui.ActionRow(password, settings, logout)
            }
        };
    }

    private async Task LoadAsync()
    {
        var result = await _api.GetCurrentUserAsync();
        if (!result.Success || result.Data is null)
        {
            ShowError(_info, ErrorText(result, "Не удалось загрузить профиль."));
            return;
        }

        _user = result.Data;
        _surname.Text = _user.Surname;
        _name.Text = _user.FirstName;
        _patronymic.Text = _user.Patronymic;
        _email.Text = _user.Email;
        _avatar.Text = _user.AvatarUrl;
        ShowError(_info, string.Empty);
    }

    private async Task SaveAsync()
    {
        if (_user is null)
            return;

        _user.Surname = _surname.Text;
        _user.FirstName = _name.Text;
        _user.Patronymic = _patronymic.Text;
        _user.Email = _email.Text ?? string.Empty;
        _user.AvatarUrl = _avatar.Text;
        _user.Theme = "dark";

        var result = await _api.UpdateProfileAsync(_user);
        _info.TextColor = result.Success ? Ui.Accent : Ui.Danger;
        ShowError(_info, result.Success ? "Профиль сохранен." : ErrorText(result, "Не удалось сохранить профиль."));
    }
}
