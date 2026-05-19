using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class RegisterPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly Entry _surname = Ui.Entry("Фамилия");
    private readonly Entry _name = Ui.Entry("Имя");
    private readonly Entry _patronymic = Ui.Entry("Отчество");
    private readonly Entry _email = Ui.Entry("Email", Keyboard.Email);
    private readonly Entry _password = Ui.Entry("Пароль", isPassword: true);
    private readonly Entry _code = Ui.Entry("Код из письма", Keyboard.Numeric);
    private readonly Picker _specialty = Ui.Picker("Специальность");
    private readonly Picker _group = Ui.Picker("Группа");
    private readonly Label _error = ErrorLabel();
    private readonly Label _success = new() { TextColor = Ui.Accent, IsVisible = false };

    protected override bool AllowAnonymous => true;

    public RegisterPage()
    {
        Title = "Регистрация";

        _specialty.SelectedIndexChanged += async (_, _) => await LoadGroupsAsync();

        var sendCode = Ui.SecondaryButton("Отправить код");
        sendCode.Clicked += async (_, _) => await SendCodeAsync();

        var register = Ui.PrimaryButton("Создать аккаунт");
        register.Clicked += async (_, _) => await RegisterAsync();

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Регистрация", "Создание студенческого аккаунта с подтверждением по почте."),
                Ui.Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        Ui.Eyebrow("Личные данные"),
                        _surname,
                        _name,
                        _patronymic,
                        _email,
                        _password,
                        Ui.Eyebrow("Учебная группа"),
                        _specialty,
                        _group,
                        Ui.Eyebrow("Подтверждение"),
                        _code,
                        _error,
                        _success,
                        Ui.ActionRow(sendCode, register)
                    }
                })
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_specialty.ItemsSource is null)
            await LoadSpecialtiesAsync();
    }

    private async Task LoadSpecialtiesAsync()
    {
        var result = await _api.GetSpecialtiesAsync();
        if (result.Success && result.Data is not null)
            _specialty.ItemsSource = result.Data;
        else
            ShowError(_error, ErrorText(result, "Не удалось загрузить специальности."));
    }

    private async Task LoadGroupsAsync()
    {
        _group.ItemsSource = null;
        if (_specialty.SelectedItem is not SpecialtyOption option)
            return;

        var result = await _api.GetGroupsAsync(option.Id);
        if (result.Success && result.Data is not null)
            _group.ItemsSource = result.Data;
        else
            ShowError(_error, ErrorText(result, "Не удалось загрузить группы."));
    }

    private RegisterRequest BuildRequest()
    {
        return new RegisterRequest
        {
            Surname = _surname.Text ?? string.Empty,
            Name = _name.Text ?? string.Empty,
            Patronymic = _patronymic.Text,
            Email = _email.Text ?? string.Empty,
            Password = _password.Text ?? string.Empty,
            GroupId = (_group.SelectedItem as GroupOption)?.Id,
            Code = _code.Text
        };
    }

    private async Task SendCodeAsync()
    {
        ShowError(_error, string.Empty);
        _success.IsVisible = false;
        await RunBusyAsync(async () =>
        {
            var result = await _api.SendRegistrationCodeAsync(BuildRequest());
            if (!result.Success)
            {
                ShowError(_error, ErrorText(result, "Не удалось отправить код."));
                return;
            }

            _success.Text = "Код отправлен на почту.";
            _success.IsVisible = true;
        });
    }

    private async Task RegisterAsync()
    {
        ShowError(_error, string.Empty);
        await RunBusyAsync(async () =>
        {
            var result = await _api.RegisterAsync(BuildRequest());
            if (!result.Success || result.Data is null)
            {
                ShowError(_error, ErrorText(result, "Не удалось зарегистрироваться."));
                return;
            }

            await _session.SignInAsync(result.Data);
            if (Shell.Current is AppShell shell)
                await shell.SignInToStudentAreaAsync();
        });
    }
}
