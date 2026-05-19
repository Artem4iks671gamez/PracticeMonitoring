using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ChangePasswordPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly Entry _current = Ui.Entry("Текущий пароль", isPassword: true);
    private readonly Entry _new = Ui.Entry("Новый пароль", isPassword: true);
    private readonly Label _message = ErrorLabel();

    public ChangePasswordPage()
    {
        Title = "Пароль";

        var save = Ui.PrimaryButton("Сохранить пароль");
        save.Clicked += async (_, _) => await SaveAsync();

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Смена пароля", "Если пароль временный, поле текущего пароля можно оставить пустым."),
                Ui.Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        _current,
                        _new,
                        _message,
                        save
                    }
                })
            }
        });
    }

    private async Task SaveAsync()
    {
        if (!await RequireStudentAsync())
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _api.ChangePasswordAsync(_current.Text, _new.Text ?? string.Empty);
            if (!result.Success)
            {
                _message.TextColor = Ui.Danger;
                ShowError(_message, ErrorText(result, "Не удалось сменить пароль."));
                return;
            }

            _message.TextColor = Ui.Accent;
            ShowError(_message, "Пароль изменен.");

            if (Shell.Current is AppShell shell)
                await shell.SignInToStudentAreaAsync();
        });
    }
}
