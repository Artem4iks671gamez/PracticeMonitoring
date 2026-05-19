using System.Collections.ObjectModel;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class NotificationsPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly ObservableCollection<NotificationItem> _items = new();
    private readonly Label _message = ErrorLabel();

    public NotificationsPage()
    {
        Title = "Уведомления";

        var list = new CollectionView
        {
            ItemsSource = _items,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = Ui.Title("", 16);
                title.SetBinding(Label.TextProperty, nameof(NotificationItem.Title));

                var text = Ui.Caption("");
                text.SetBinding(Label.TextProperty, nameof(NotificationItem.Message));

                var date = Ui.Caption("");
                date.SetBinding(Label.TextProperty, new Binding(nameof(NotificationItem.CreatedAtUtc), stringFormat: "{0:dd.MM.yyyy HH:mm}"));

                var read = new Label
                {
                    TextColor = Ui.PrimarySoft,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                };
                read.SetBinding(Label.TextProperty, new Binding(nameof(NotificationItem.IsRead), converter: new ReadStateConverter()));

                return Ui.Card(new VerticalStackLayout
                {
                    Spacing = 7,
                    Children = { read, title, text, date }
                });
            })
        };

        list.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is NotificationItem item)
            {
                list.SelectedItem = null;
                await _api.MarkNotificationReadAsync(item.Id);
                await LoadAsync();
            }
        };

        var refresh = Ui.SecondaryButton("Обновить");
        refresh.Clicked += async (_, _) => await LoadAsync();

        var all = Ui.PrimaryButton("Прочитать все");
        all.Clicked += async (_, _) =>
        {
            await _api.MarkAllNotificationsReadAsync();
            await LoadAsync();
        };

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Уведомления", "Сообщения системы, проверки дневника и изменения по практикам."),
                _message,
                Ui.ActionRow(refresh, all),
                list
            }
        });
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
        await RunBusyAsync(async () =>
        {
            var result = await _api.GetNotificationsAsync();
            _items.Clear();
            if (!result.Success || result.Data is null)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить уведомления."));
                return;
            }

            foreach (var item in result.Data.OrderByDescending(x => x.CreatedAtUtc))
                _items.Add(item);

            ShowError(_message, _items.Count == 0 ? "Уведомлений пока нет." : string.Empty);
        });
    }

    private sealed class ReadStateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is true ? "ПРОЧИТАНО" : "НОВОЕ";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }
}
