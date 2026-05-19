using System.Collections.ObjectModel;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ChatsPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly ObservableCollection<ChatThreadItem> _threads = new();
    private readonly Label _message = ErrorLabel();

    public ChatsPage()
    {
        Title = "Чаты";

        var list = new CollectionView
        {
            ItemsSource = _threads,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = Ui.Title("", 16);
                title.SetBinding(Label.TextProperty, "OtherUser.FullName");

                var subtitle = Ui.Caption("");
                subtitle.SetBinding(Label.TextProperty, "OtherUser.Subtitle");

                var last = Ui.Caption("");
                last.SetBinding(Label.TextProperty, nameof(ChatThreadItem.LastMessagePreview));

                var unread = new Label
                {
                    TextColor = Ui.Accent,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                };
                unread.SetBinding(Label.TextProperty, new Binding(nameof(ChatThreadItem.UnreadCount), stringFormat: "Непрочитано: {0}"));

                return Ui.Card(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { title, subtitle, last, unread }
                });
            })
        };
        list.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is ChatThreadItem item)
            {
                list.SelectedItem = null;
                await Shell.Current.GoToAsync($"{nameof(ChatThreadPage)}?threadId={item.Id}");
            }
        };

        var refresh = Ui.SecondaryButton("Обновить");
        refresh.Clicked += async (_, _) => await LoadAsync();

        var search = Ui.PrimaryButton("Новый диалог");
        search.Clicked += async (_, _) => await SearchContactAsync();

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Чаты", "Общение с руководителем и участниками практики."),
                _message,
                Ui.ActionRow(refresh, search),
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
            var result = await _api.GetThreadsAsync();
            _threads.Clear();
            if (!result.Success || result.Data is null)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить диалоги."));
                return;
            }

            foreach (var item in result.Data.OrderByDescending(x => x.LastMessageAtUtc ?? DateTime.MinValue))
                _threads.Add(item);

            ShowError(_message, _threads.Count == 0 ? "Диалогов пока нет." : string.Empty);
        });
    }

    private async Task SearchContactAsync()
    {
        var query = await DisplayPromptAsync("Новый диалог", "Введите имя или email");
        if (string.IsNullOrWhiteSpace(query))
            return;

        var result = await _api.SearchContactsAsync(query);
        if (!result.Success || result.Data is null || result.Data.Count == 0)
        {
            await DisplayAlert("Поиск", ErrorText(result, "Контакты не найдены."), "ОК");
            return;
        }

        var names = result.Data.Select(x => x.ToString()).ToArray();
        var chosen = await DisplayActionSheet("Выберите контакт", "Отмена", null, names);
        var user = result.Data.FirstOrDefault(x => x.ToString() == chosen);
        if (user is null)
            return;

        var thread = await _api.StartThreadAsync(user.Id);
        if (thread.Success && thread.Data is not null)
            await Shell.Current.GoToAsync($"{nameof(ChatThreadPage)}?threadId={thread.Data.Id}");
        else
            await DisplayAlert("Диалог", ErrorText(thread, "Не удалось открыть диалог."), "ОК");
    }
}
