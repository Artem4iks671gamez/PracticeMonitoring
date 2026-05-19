using System.Collections.ObjectModel;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

[QueryProperty(nameof(ThreadId), "threadId")]
public sealed class ChatThreadPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly ObservableCollection<ChatMessage> _messages = new();
    private readonly Entry _text = Ui.Entry("Сообщение");
    private readonly Label _message = ErrorLabel();
    private int _threadId;
    private FileResult? _attachment;

    public string ThreadId
    {
        set => _threadId = int.TryParse(value, out var id) ? id : 0;
    }

    public ChatThreadPage()
    {
        Title = "Диалог";

        var list = new CollectionView
        {
            ItemsSource = _messages,
            ItemTemplate = new DataTemplate(() =>
            {
                var author = Ui.Eyebrow("");
                author.SetBinding(Label.TextProperty, nameof(ChatMessage.SenderFullName));

                var text = Ui.Title("", 15);
                text.SetBinding(Label.TextProperty, nameof(ChatMessage.Text));

                var date = Ui.Caption("");
                date.SetBinding(Label.TextProperty, new Binding(nameof(ChatMessage.CreatedAtUtc), stringFormat: "{0:dd.MM.yyyy HH:mm}"));

                return Ui.Card(new VerticalStackLayout
                {
                    Spacing = 5,
                    Children = { author, text, date }
                });
            })
        };

        var attach = Ui.SecondaryButton("Файл");
        attach.Clicked += async (_, _) => _attachment = await FilePicker.Default.PickAsync();

        var send = Ui.PrimaryButton("Отправить");
        send.Clicked += async (_, _) => await SendAsync();

        var inputPanel = Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children = { _text, Ui.ActionRow(attach, send) }
        }, new Thickness(12));
        Grid.SetRow(inputPanel, 1);

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            BackgroundColor = Ui.PageBackground,
            Children =
            {
                new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = 16,
                        Spacing = 10,
                        Children = { _message, list }
                    }
                },
                inputPanel
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await RequireStudentAsync())
            return;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_threadId <= 0)
            return;

        var result = await _api.GetThreadAsync(_threadId);
        _messages.Clear();
        if (!result.Success || result.Data is null)
        {
            ShowError(_message, ErrorText(result, "Не удалось загрузить диалог."));
            return;
        }

        Title = result.Data.OtherUser.FullName;
        foreach (var item in result.Data.Messages.OrderBy(x => x.CreatedAtUtc))
            _messages.Add(item);
    }

    private async Task SendAsync()
    {
        if (!await RequireStudentAsync())
            return;

        var result = await _api.SendMessageAsync(_threadId, _text.Text ?? string.Empty, _attachment);
        if (!result.Success)
        {
            ShowError(_message, ErrorText(result, "Не удалось отправить сообщение."));
            return;
        }

        _text.Text = string.Empty;
        _attachment = null;
        await LoadAsync();
    }
}
