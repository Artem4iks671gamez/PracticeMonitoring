using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class SettingsPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly Entry _apiUrl = Ui.Entry("API URL");
    private readonly Entry _webUrl = Ui.Entry("Web URL для документов");
    private readonly Label _message = ErrorLabel();

    protected override bool AllowAnonymous => true;

    public SettingsPage()
    {
        Title = "Подключение";
        _apiUrl.Text = _api.ApiBaseUrl;
        _webUrl.Text = _api.WebBaseUrl;

        var save = Ui.PrimaryButton("Сохранить");
        save.Clicked += (_, _) =>
        {
            _api.ApiBaseUrl = _apiUrl.Text ?? string.Empty;
            _api.WebBaseUrl = _webUrl.Text ?? string.Empty;
            _message.TextColor = Ui.Accent;
            ShowError(_message, "Настройки сохранены.");
        };

        var check = Ui.SecondaryButton("Проверить API");
        check.Clicked += async (_, _) =>
        {
            _api.ApiBaseUrl = _apiUrl.Text ?? string.Empty;
            var result = await _api.GetSpecialtiesAsync();
            _message.TextColor = result.Success ? Ui.Accent : Ui.Danger;
            ShowError(_message, result.Success ? "API доступен." : ErrorText(result, "API недоступен."));
        };

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Подключение", "Для Android-эмулятора локальный API обычно открывается через 10.0.2.2."),
                Ui.Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        Ui.Eyebrow("API"),
                        _apiUrl,
                        Ui.Caption("Локально для эмулятора: http://10.0.2.2:5149/"),
                        Ui.Eyebrow("Документы"),
                        _webUrl,
                        Ui.Caption("Web URL нужен для скачивания DOCX/PDF, пока генерация документов остается в веб-приложении."),
                        _message,
                        Ui.ActionRow(save, check)
                    }
                })
            }
        });
    }
}
