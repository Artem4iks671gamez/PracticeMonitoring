using System.Collections.ObjectModel;
using System.Globalization;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class PracticesPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly ObservableCollection<PracticeListItem> _items = new();
    private readonly CollectionView _list;
    private readonly Label _message = ErrorLabel();

    public PracticesPage()
    {
        Title = "Практики";

        _list = new CollectionView
        {
            ItemsSource = _items,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var index = new Label
                {
                    TextColor = Ui.PrimarySoft,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                };
                index.SetBinding(Label.TextProperty, nameof(PracticeListItem.PracticeIndex));

                var status = new Label
                {
                    TextColor = Ui.Accent,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.End
                };
                status.SetBinding(Label.TextProperty, new Binding(nameof(PracticeListItem.IsCompleted), converter: new PracticeStatusConverter()));

                var top = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Children = { index, status }
                };
                Grid.SetColumn(status, 1);

                var title = Ui.Title("", 18);
                title.SetBinding(Label.TextProperty, nameof(PracticeListItem.Name));

                var meta = Ui.Caption("");
                meta.SetBinding(Label.TextProperty, new Binding(".", converter: new PracticeMetaConverter()));

                var progress = new ProgressBar
                {
                    ProgressColor = Ui.Accent,
                    BackgroundColor = Ui.SurfaceInput,
                    HeightRequest = 6
                };
                progress.SetBinding(ProgressBar.ProgressProperty, new Binding(".", converter: new PracticeProgressValueConverter()));

                var progressText = Ui.Caption("");
                progressText.SetBinding(Label.TextProperty, new Binding(".", converter: new PracticeProgressTextConverter()));

                return Ui.Card(new VerticalStackLayout
                {
                    Spacing = 8,
                    Children = { top, title, meta, progress, progressText }
                });
            })
        };
        _list.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is PracticeListItem item)
            {
                _list.SelectedItem = null;
                await Shell.Current.GoToAsync($"{nameof(PracticeDetailsPage)}?assignmentId={item.AssignmentId}");
            }
        };

        var refresh = Ui.SecondaryButton("Обновить");
        refresh.Clicked += async (_, _) => await LoadAsync();

        Content = Scroll(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Hero("Мои практики", "Откройте практику, чтобы заполнить дневник, отчет, приложения и документы."),
                _message,
                refresh,
                _list
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
        ShowError(_message, string.Empty);
        await RunBusyAsync(async () =>
        {
            var result = await _api.GetPracticesAsync();
            _items.Clear();
            if (!result.Success || result.Data is null)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить практики."));
                return;
            }

            foreach (var item in result.Data.OrderByDescending(x => x.StartDate))
                _items.Add(item);

            if (_items.Count == 0)
                ShowError(_message, "Назначенных практик пока нет.");
        });
    }

    private sealed class PracticeMetaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is PracticeListItem item
                ? $"{Ui.Date(item.StartDate)} - {Ui.Date(item.EndDate)} · {item.Hours} ч. · {item.ProfessionalModuleCode} {item.ProfessionalModuleName}".Trim()
                : string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    private sealed class PracticeStatusConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? "Завершена" : "Активна";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    private sealed class PracticeProgressTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is PracticeListItem item
                ? $"Дневник: {item.DiaryEntriesCount}/{item.WorkDaysCount} рабочих дней"
                : string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    private sealed class PracticeProgressValueConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not PracticeListItem item || item.WorkDaysCount <= 0)
                return 0d;

            return Math.Clamp((double)item.DiaryEntriesCount / item.WorkDaysCount, 0d, 1d);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
