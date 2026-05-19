using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ResetPasswordPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly Entry _email = Ui.Entry("Email", Keyboard.Email);
    private readonly Entry _code = Ui.Entry("Код из письма", Keyboard.Numeric);
    private readonly Entry _password = Ui.Entry("Новый пароль", isPassword: true);
    private readonly Label _message = ErrorLabel();

    protected override bool AllowAnonymous => true;

    public ResetPasswordPage()
    {
        Title = "Восстановление";

        var send = Ui.SecondaryButton("Отправить код");
        send.Clicked += async (_, _) => await SendCodeAsync();

        var reset = Ui.PrimaryButton("Сменить пароль");
        reset.Clicked += async (_, _) => await ResetAsync();

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Восстановление", "Код придет на почту, указанную в аккаунте."),
                Ui.Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        _email,
                        _code,
                        _password,
                        _message,
                        Ui.ActionRow(send, reset)
                    }
                })
            }
        });
    }

    private async Task SendCodeAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _api.ForgotPasswordAsync(_email.Text ?? string.Empty);
            _message.TextColor = result.Success ? Ui.Accent : Ui.Danger;
            ShowError(_message, result.Success ? "Если аккаунт существует, код отправлен." : ErrorText(result, "Не удалось отправить код."));
        });
    }

    private async Task ResetAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _api.ResetPasswordAsync(_email.Text ?? string.Empty, _code.Text ?? string.Empty, _password.Text ?? string.Empty);
            _message.TextColor = result.Success ? Ui.Accent : Ui.Danger;
            ShowError(_message, result.Success ? "Пароль изменен. Можно войти." : ErrorText(result, "Не удалось сменить пароль."));
        });
    }
}
