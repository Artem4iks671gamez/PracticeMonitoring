using Microsoft.Maui.Controls.Shapes;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

[QueryProperty(nameof(ThreadId), "threadId")]
public sealed class ChatThreadPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly VerticalStackLayout _messages = new() { Spacing = 8, Padding = new Thickness(16, 12) };
    private readonly ScrollView _scroll;
    private readonly Entry _text = Ui.Entry("Сообщение");
    private readonly Label _message = ErrorLabel();
    private readonly Label _attachmentLabel = Ui.Caption(string.Empty);
    private int _threadId;
    private int _currentUserId;
    private FileResult? _attachment;

    public string ThreadId
    {
        set => _threadId = int.TryParse(value, out var id) ? id : 0;
    }

    public ChatThreadPage()
    {
        Title = "Диалог";

        _scroll = new ScrollView
        {
            BackgroundColor = Ui.PageBackground,
            Content = _messages
        };

        var inputPanel = Composer();
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
                _scroll,
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

    private View Composer()
    {
        var attach = Ui.SecondaryButton("+");
        attach.WidthRequest = 48;
        attach.Clicked += async (_, _) =>
        {
            _attachment = await FilePicker.Default.PickAsync();
            _attachmentLabel.Text = _attachment is null ? string.Empty : $"Файл: {_attachment.FileName}";
            _attachmentLabel.IsVisible = _attachment is not null;
        };

        var send = Ui.PrimaryButton("Отправить");
        send.WidthRequest = 118;
        send.Clicked += async (_, _) => await SendAsync();

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        AddGrid(row, attach, 0, 0);
        AddGrid(row, _text, 1, 0);
        AddGrid(row, send, 2, 0);

        _attachmentLabel.IsVisible = false;

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children = { _message, _attachmentLabel, row }
        }, new Thickness(12, 10));
    }

    private async Task LoadAsync()
    {
        if (_threadId <= 0)
            return;

        await EnsureCurrentUserAsync();

        var result = await _api.GetThreadAsync(_threadId);
        _messages.Children.Clear();
        if (!result.Success || result.Data is null)
        {
            ShowError(_message, ErrorText(result, "Не удалось загрузить диалог."));
            return;
        }

        Title = result.Data.OtherUser.FullName;
        ShowError(_message, string.Empty);

        if (result.Data.Messages.Count == 0)
        {
            _messages.Children.Add(Ui.EmptyState("Сообщений пока нет", "Напишите первое сообщение в этом диалоге."));
        }
        else
        {
            foreach (var item in result.Data.Messages.OrderBy(x => x.CreatedAtUtc))
                _messages.Children.Add(MessageBubble(item));
        }

        await Task.Delay(50);
        await _scroll.ScrollToAsync(_messages, ScrollToPosition.End, animated: false);
    }

    private View MessageBubble(ChatMessage item)
    {
        var own = _currentUserId > 0 && item.SenderUserId == _currentUserId;
        var content = new VerticalStackLayout { Spacing = 4 };

        if (!own)
            content.Children.Add(Ui.Eyebrow(item.SenderFullName));

        if (!string.IsNullOrWhiteSpace(item.Text))
        {
            content.Children.Add(new Label
            {
                Text = item.Text,
                TextColor = own ? Colors.White : Ui.Text,
                FontSize = 15,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        foreach (var attachment in item.Attachments)
            content.Children.Add(Ui.Caption($"Файл: {attachment.FileName}"));

        content.Children.Add(new Label
        {
            Text = item.CreatedAtUtc.ToLocalTime().ToString("dd.MM HH:mm"),
            TextColor = own ? Color.FromArgb("#DCE6FF") : Ui.Faint,
            FontSize = 11,
            HorizontalTextAlignment = own ? TextAlignment.End : TextAlignment.Start
        });

        var bubble = new Border
        {
            BackgroundColor = own ? Ui.Primary : Ui.SurfaceRaised,
            Stroke = own ? Ui.Primary : Ui.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 9),
            MaximumWidthRequest = 320,
            Content = content
        };

        return new HorizontalStackLayout
        {
            HorizontalOptions = own ? LayoutOptions.End : LayoutOptions.Start,
            Children = { bubble }
        };
    }

    private async Task SendAsync()
    {
        if (!await RequireStudentAsync())
            return;

        var text = _text.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) && _attachment is null)
        {
            ShowError(_message, "Введите сообщение или прикрепите файл.");
            return;
        }

        var result = await _api.SendMessageAsync(_threadId, text, _attachment);
        if (!result.Success)
        {
            ShowError(_message, ErrorText(result, "Не удалось отправить сообщение."));
            return;
        }

        _text.Text = string.Empty;
        _attachment = null;
        _attachmentLabel.Text = string.Empty;
        _attachmentLabel.IsVisible = false;
        await LoadAsync();
    }

    private async Task EnsureCurrentUserAsync()
    {
        if (_currentUserId > 0)
            return;

        if (_session.CurrentUser is not null)
        {
            _currentUserId = _session.CurrentUser.Id;
            return;
        }

        var result = await _api.GetCurrentUserAsync();
        if (result.Success && result.Data is not null)
        {
            _session.CurrentUser = result.Data;
            _currentUserId = result.Data.Id;
        }
    }

    private static void AddGrid(Grid grid, View view, int column, int row)
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        grid.Children.Add(view);
    }
}
