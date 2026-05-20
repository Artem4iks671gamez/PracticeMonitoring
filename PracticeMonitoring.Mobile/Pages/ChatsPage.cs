using Microsoft.Maui.Controls.Shapes;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ChatsPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly VerticalStackLayout _root = new() { Spacing = 14 };
    private readonly Entry _search = Ui.Entry("Имя, email, группа");
    private readonly Label _message = ErrorLabel();
    private List<ChatThreadItem> _threads = new();
    private List<ChatUser> _contacts = new();

    public ChatsPage()
    {
        Title = "Чаты";
        _search.Completed += async (_, _) => await LoadAsync();
        Content = Scroll(_root);
        Render();
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
            var threadsTask = _api.GetThreadsAsync();
            var contactsTask = _api.SearchContactsAsync(_search.Text ?? string.Empty);
            await Task.WhenAll(threadsTask, contactsTask);

            var threads = threadsTask.Result;
            var contacts = contactsTask.Result;

            _threads = threads.Success && threads.Data is not null
                ? threads.Data.OrderByDescending(x => x.LastMessageAtUtc ?? DateTime.MinValue).ToList()
                : new List<ChatThreadItem>();

            _contacts = contacts.Success && contacts.Data is not null
                ? contacts.Data.OrderBy(x => x.FullName).ToList()
                : new List<ChatUser>();

            _message.TextColor = Ui.Danger;
            ShowError(_message, !threads.Success
                ? ErrorText(threads, "Не удалось загрузить диалоги.")
                : !contacts.Success
                    ? ErrorText(contacts, "Не удалось загрузить контакты.")
                    : string.Empty);

            Render();
        });
    }

    private void Render()
    {
        Ui.Detach(_search);
        Ui.Detach(_message);
        _root.Children.Clear();

        var unread = _threads.Sum(x => x.UnreadCount);
        _root.Children.Add(Ui.Hero(
            "Сообщения",
            unread > 0 ? $"Непрочитанных сообщений: {unread}" : "Диалоги и доступные контакты"));

        _root.Children.Add(SearchPanel());
        _root.Children.Add(_message);
        _root.Children.Add(DialogsSection());
        _root.Children.Add(ContactsSection());
    }

    private View SearchPanel()
    {
        Ui.Detach(_search);

        var refresh = Ui.SecondaryButton("Обновить");
        refresh.Clicked += async (_, _) => await LoadAsync();

        var find = Ui.PrimaryButton("Найти");
        find.Clicked += async (_, _) => await LoadAsync();

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Eyebrow("Поиск контактов"),
                _search,
                Ui.ActionRow(refresh, find)
            }
        }, new Thickness(12));
    }

    private View DialogsSection()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(SectionHeader("Диалоги", _threads.Count == 0 ? "Нет активных" : $"{_threads.Count}"));

        if (_threads.Count == 0)
        {
            stack.Children.Add(Ui.EmptyState("Диалогов пока нет", "Выберите человека из списка ниже и начните переписку."));
            return stack;
        }

        foreach (var thread in _threads)
            stack.Children.Add(ThreadCard(thread));

        return stack;
    }

    private View ContactsSection()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(SectionHeader("Кому можно написать", _contacts.Count == 0 ? "Нет контактов" : $"{_contacts.Count}"));

        if (_contacts.Count == 0)
        {
            stack.Children.Add(Ui.EmptyState("Контакты не найдены", "Измените поисковый запрос или обновите список."));
            return stack;
        }

        foreach (var contact in _contacts)
            stack.Children.Add(ContactCard(contact));

        return stack;
    }

    private static View SectionHeader(string title, string meta)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        AddGrid(grid, Ui.Title(title, 18), 0, 0);
        AddGrid(
            grid,
            new Border
            {
                BackgroundColor = Color.FromRgba(Ui.PrimarySoft.Red, Ui.PrimarySoft.Green, Ui.PrimarySoft.Blue, 0.14),
                Stroke = Color.FromRgba(Ui.PrimarySoft.Red, Ui.PrimarySoft.Green, Ui.PrimarySoft.Blue, 0.28),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(10, 4),
                Content = new Label
                {
                    Text = meta,
                    TextColor = Ui.PrimarySoft,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                }
            },
            1,
            0);

        return grid;
    }

    private View ThreadCard(ChatThreadItem thread)
    {
        var card = ChatCardShell(thread.OtherUser, ThreadMeta(thread), thread.LastMessagePreview, thread.UnreadCount);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OpenThreadAsync(thread.Id))
        });

        return card;
    }

    private View ContactCard(ChatUser contact)
    {
        var write = Ui.PrimaryButton("Написать");
        write.WidthRequest = 116;
        write.HeightRequest = 44;
        write.FontSize = 13;
        write.Clicked += async (_, _) => await StartThreadAsync(contact);

        var card = ChatCardShell(contact, contact.Email, contact.Subtitle, 0, write);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await StartThreadAsync(contact))
        });

        return card;
    }

    private static Border ChatCardShell(ChatUser user, string meta, string preview, int unreadCount, View? action = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            RowSpacing = 4
        };

        AddGrid(grid, Avatar(user.FullName), 0, 0);

        var content = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                Ui.Title(user.FullName, 16),
                Ui.Caption(string.IsNullOrWhiteSpace(preview) ? "Диалог создан" : preview),
                Ui.Caption(meta)
            }
        };
        AddGrid(grid, content, 1, 0);

        if (action is not null)
        {
            AddGrid(grid, action, 2, 0);
        }
        else if (unreadCount > 0)
        {
            AddGrid(grid, UnreadPill(unreadCount), 2, 0);
        }

        return Ui.Card(grid, new Thickness(12));
    }

    private static View Avatar(string fullName)
    {
        return new Border
        {
            WidthRequest = 50,
            HeightRequest = 50,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 25 },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Ui.Primary, 0),
                    new GradientStop(Ui.Accent, 1)
                }
            },
            Content = new Label
            {
                Text = Initials(fullName),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View UnreadPill(int unreadCount)
    {
        return new Border
        {
            BackgroundColor = Ui.Accent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = unreadCount > 99 ? "99+" : unreadCount.ToString(),
                TextColor = Ui.PageBackground,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            }
        };
    }

    private async Task OpenThreadAsync(int threadId)
    {
        if (threadId <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(ChatThreadPage)}?threadId={threadId}");
    }

    private async Task StartThreadAsync(ChatUser user)
    {
        var result = await _api.StartThreadAsync(user.Id);
        if (result.Success && result.Data is not null && result.Data.Id > 0)
        {
            await Shell.Current.GoToAsync($"{nameof(ChatThreadPage)}?threadId={result.Data.Id}");
            return;
        }

        _message.TextColor = Ui.Danger;
        ShowError(_message, ErrorText(result, "Не удалось открыть диалог."));
    }

    private static string ThreadMeta(ChatThreadItem thread)
    {
        return thread.LastMessageAtUtc.HasValue
            ? $"{thread.OtherUser.Subtitle} · {thread.LastMessageAtUtc.Value:dd.MM HH:mm}"
            : thread.OtherUser.Subtitle;
    }

    private static string Initials(string value)
    {
        var parts = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(x => x[0].ToString())
            .ToArray();

        return parts.Length == 0 ? "PM" : string.Concat(parts).ToUpperInvariant();
    }

    private static void AddGrid(Grid grid, View view, int column, int row)
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        grid.Children.Add(view);
    }
}
