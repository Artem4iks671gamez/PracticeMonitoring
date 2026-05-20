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

    private View AvatarBadge(CurrentUser user)
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

        var avatarUrl = ResolveAvatarUrl(user.AvatarUrl);
        View content = string.IsNullOrWhiteSpace(avatarUrl)
            ? new Label
            {
                Text = initials,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
            : new Image
            {
                Source = ImageSource.FromUri(new Uri(avatarUrl)),
                Aspect = Aspect.AspectFill
            };

        return new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            StrokeThickness = 0,
            BackgroundColor = Ui.Primary,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = content
        };
    }

    private string? ResolveAvatarUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            return trimmed;

        return new Uri(new Uri(_api.WebBaseUrl), trimmed.TrimStart('/')).ToString();
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

    private View PracticeSummary(IReadOnlyCollection<PracticeListItem> practices)
    {
        var active = practices.Count(x => !x.IsCompleted);
        var completed = practices.Count(x => x.IsCompleted);
        var overdue = practices.Count(x => x.IsDetailsOverdue);
        var readyDetails = practices.Count(x => x.HasRequiredDetails);
        var diary = practices.Sum(x => x.DiaryEntriesCount);
        var days = practices.Sum(x => x.WorkDaysCount);
        var diaryPercent = days <= 0 ? 0 : Math.Round(diary * 100d / days);
        var ordered = practices
            .OrderBy(x => x.IsCompleted)
            .ThenByDescending(x => x.IsDetailsOverdue)
            .ThenBy(x => x.StartDate)
            .ToList();

        var list = new VerticalStackLayout { Spacing = 10 };
        if (ordered.Count == 0)
        {
            list.Children.Add(Ui.EmptyState("Назначенных практик пока нет", "Когда отдел назначит практику, здесь появятся сроки, дневник и готовность сведений."));
        }
        else
        {
            foreach (var practice in ordered.Take(4))
                list.Children.Add(PracticeMiniCard(practice));

            if (ordered.Count > 4)
                list.Children.Add(Ui.Caption($"Ещё практик: {ordered.Count - 4}. Полный список во вкладке «Практики»."));
        }

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
                    Ui.Metric("Дневник", days == 0 ? "0%" : $"{diaryPercent:0}%"),
                    Ui.Metric("Готово", $"{readyDetails}/{practices.Count}")),
                Ui.TwoColumns(
                    Ui.Metric("Завершены", completed.ToString()),
                    Ui.Metric("Просрочки", overdue.ToString())),
                list
            }
        });
    }

    private View PracticeMiniCard(PracticeListItem practice)
    {
        var progress = practice.WorkDaysCount <= 0
            ? 0
            : Math.Clamp(practice.DiaryEntriesCount / (double)practice.WorkDaysCount, 0, 1);

        var statusColor = practice.IsCompleted
            ? Ui.Accent
            : practice.IsDetailsOverdue
                ? Ui.Warning
                : practice.HasRequiredDetails
                    ? Ui.PrimarySoft
                    : Ui.Danger;

        var status = practice.IsCompleted
            ? "Завершена"
            : practice.IsDetailsOverdue
                ? "Просрочены сведения"
                : practice.HasRequiredDetails
                    ? "Сведения готовы"
                    : "Нужно заполнить";

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        var title = Ui.Title($"{FormatPracticeIndex(practice.PracticeIndex)} {practice.Name}".Trim(), 15);
        var chip = Ui.Chip(status, statusColor);
        Grid.SetColumn(title, 0);
        Grid.SetColumn(chip, 1);
        header.Children.Add(title);
        header.Children.Add(chip);

        var card = Ui.Panel(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                header,
                Ui.Caption($"{Ui.Date(practice.StartDate)} - {Ui.Date(practice.EndDate)} · {practice.Hours} ч."),
                Ui.Caption(string.IsNullOrWhiteSpace(practice.OrganizationName) ? "Организация не указана" : practice.OrganizationName),
                new ProgressBar
                {
                    Progress = progress,
                    ProgressColor = Ui.Accent,
                    BackgroundColor = Ui.SurfaceInput,
                    HeightRequest = 6
                },
                Ui.Caption($"Дневник: {practice.DiaryEntriesCount}/{practice.WorkDaysCount} рабочих дней")
            }
        });

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(PracticeDetailsPage)}?assignmentId={practice.AssignmentId}"))
        });

        return card;
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

    private static string FormatPracticeIndex(string? value)
    {
        var normalized = StripAcademicPrefix(value, "ПП");
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"ПП.{normalized}";
    }

    private static string StripAcademicPrefix(string? value, string prefix)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[prefix.Length..].TrimStart();
            if (normalized.StartsWith(".", StringComparison.Ordinal))
                normalized = normalized[1..].TrimStart();
        }

        return normalized;
    }
}
