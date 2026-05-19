using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class DashboardPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly VerticalStackLayout _content = new() { Spacing = 14 };

    public DashboardPage()
    {
        Title = "Главная";
        Content = Scroll(_content);
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
        _content.Children.Clear();

        await RunBusyAsync(async () =>
        {
            var user = await _api.GetCurrentUserAsync();
            if (!user.Success || user.Data is null)
            {
                _content.Children.Add(Ui.EmptyState("Профиль недоступен", ErrorText(user, "Не удалось загрузить профиль.")));
                return;
            }

            _session.CurrentUser = user.Data;
            _content.Children.Add(Ui.Hero("Кабинет студента", user.Data.FullName, AvatarBadge(user.Data)));
            _content.Children.Add(ProfileSummary(user.Data));

            var practices = await _api.GetPracticesAsync();
            if (practices.Success && practices.Data is not null)
                _content.Children.Add(PracticeSummary(practices.Data));
            else
                _content.Children.Add(Ui.EmptyState("Практики недоступны", ErrorText(practices, "Не удалось загрузить практики.")));

            var notifications = await _api.GetNotificationsAsync();
            if (notifications.Success && notifications.Data is not null)
                _content.Children.Add(NotificationSummary(notifications.Data));
        });
    }

    private static View AvatarBadge(CurrentUser user)
    {
        var initials = string.Join("", new[]
            {
                user.Surname?.FirstOrDefault().ToString(),
                user.FirstName?.FirstOrDefault().ToString()
            })
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(initials))
            initials = "PM";

        return new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            StrokeThickness = 0,
            BackgroundColor = Ui.Primary,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = initials,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View ProfileSummary(CurrentUser user)
    {
        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Eyebrow("Профиль"),
                Ui.Title(user.Email, 18),
                Ui.Caption($"{user.SpecialtyCode} {user.SpecialtyName}".Trim()),
                Ui.Caption(user.GroupName ?? "Группа не указана"),
                Ui.Chip(user.MustChangePassword ? "Нужно сменить пароль" : "Аккаунт активен", user.MustChangePassword ? Ui.Warning : Ui.Accent)
            }
        });
    }

    private static View PracticeSummary(IReadOnlyCollection<PracticeListItem> practices)
    {
        var active = practices.Count(x => !x.IsCompleted);
        var diary = practices.Sum(x => x.DiaryEntriesCount);
        var days = practices.Sum(x => x.WorkDaysCount);

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Eyebrow("Практики"),
                Ui.TwoColumns(
                    Ui.Metric("Всего", practices.Count.ToString()),
                    Ui.Metric("Активные", active.ToString())),
                Ui.TwoColumns(
                    Ui.Metric("Дневник", $"{diary}/{days}"),
                    Ui.Metric("Статус", practices.Count == 0 ? "Нет" : "В работе")),
                Ui.Caption(practices.Count == 0
                    ? "Назначенных практик пока нет."
                    : "Откройте вкладку «Практики», чтобы заполнить сведения, дневник, отчет и скачать документы.")
            }
        });
    }

    private static View NotificationSummary(IReadOnlyCollection<NotificationItem> notifications)
    {
        var unread = notifications.Count(x => !x.IsRead);
        var latest = notifications.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.Eyebrow("Уведомления"),
                Ui.Title(unread == 0 ? "Новых нет" : $"Непрочитанных: {unread}", 18),
                Ui.Caption(latest is null ? "Уведомлений пока нет." : latest.Title)
            }
        });
    }
}
