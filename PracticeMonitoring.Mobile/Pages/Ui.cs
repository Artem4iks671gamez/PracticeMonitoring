using Microsoft.Maui.Controls.Shapes;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public static class Ui
{
    public static readonly Color PageBackground = Color.FromArgb("#0F1724");
    public static readonly Color Surface = Color.FromArgb("#1A2638");
    public static readonly Color SurfaceRaised = Color.FromArgb("#22334B");
    public static readonly Color SurfaceInput = Color.FromArgb("#111A29");
    public static readonly Color Border = Color.FromArgb("#30435F");
    public static readonly Color Primary = Color.FromArgb("#3F6BFF");
    public static readonly Color PrimarySoft = Color.FromArgb("#8EA8FF");
    public static readonly Color Accent = Color.FromArgb("#5AD0A8");
    public static readonly Color Text = Color.FromArgb("#F7FAFF");
    public static readonly Color Muted = Color.FromArgb("#AAB7CC");
    public static readonly Color Faint = Color.FromArgb("#748299");
    public static readonly Color Danger = Color.FromArgb("#FF7B7B");
    public static readonly Color Warning = Color.FromArgb("#F7C66B");

    public static Label Title(string text, double size = 24)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = FontAttributes.Bold,
            TextColor = Text,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Label Caption(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 13,
            TextColor = Muted,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Label Eyebrow(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = PrimarySoft,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Entry Entry(string placeholder, Keyboard? keyboard = null, bool isPassword = false)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Text,
            IsPassword = isPassword,
            BackgroundColor = SurfaceInput,
            TextColor = Text,
            PlaceholderColor = Faint,
            MinimumHeightRequest = 48,
            FontSize = 15,
            Margin = new Thickness(0)
        };
    }

    public static Editor Editor(string placeholder, int lines = 4)
    {
        return new Editor
        {
            Placeholder = placeholder,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = Math.Max(110, lines * 30),
            BackgroundColor = SurfaceInput,
            TextColor = Text,
            PlaceholderColor = Faint,
            FontSize = 15
        };
    }

    public static Picker Picker(string title)
    {
        return new Picker
        {
            Title = title,
            BackgroundColor = SurfaceInput,
            TextColor = Text,
            TitleColor = Faint,
            MinimumHeightRequest = 48,
            FontSize = 15
        };
    }

    public static Button PrimaryButton(string text)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Primary,
            TextColor = Colors.White,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            FontSize = 15
        };
    }

    public static Button SecondaryButton(string text)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = SurfaceRaised,
            TextColor = PrimarySoft,
            BorderColor = Border,
            BorderWidth = 1,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            FontSize = 14
        };
    }

    public static Button GhostButton(string text)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Colors.Transparent,
            TextColor = Muted,
            CornerRadius = 8,
            HeightRequest = 44,
            FontSize = 14
        };
    }

    public static Border Card(View content, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = Surface,
            Stroke = Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = padding ?? new Thickness(16),
            Content = content
        };
    }

    public static Border Panel(View content)
    {
        return new Border
        {
            BackgroundColor = SurfaceRaised,
            Stroke = Color.FromArgb("#405579"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 14,
            Content = content
        };
    }

    public static Border Chip(string text, Color? color = null)
    {
        var tint = color ?? PrimarySoft;
        return new Border
        {
            BackgroundColor = Color.FromRgba(tint.Red, tint.Green, tint.Blue, 0.16),
            Stroke = Color.FromRgba(tint.Red, tint.Green, tint.Blue, 0.32),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = text,
                TextColor = tint,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.TailTruncation
            }
        };
    }

    public static Border Metric(string label, string value)
    {
        return Panel(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                Eyebrow(label),
                Title(value, 20)
            }
        });
    }

    public static Border Hero(string title, string subtitle, View? trailing = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        Title(title, 26),
                        Caption(subtitle)
                    }
                }
            }
        };

        if (trailing is not null)
        {
            Grid.SetColumn(trailing, 1);
            grid.Children.Add(trailing);
        }

        return new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#173275"), 0),
                    new GradientStop(Color.FromArgb("#315BAE"), 1)
                }
            },
            Stroke = Color.FromArgb("#406BBF"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(18, 18),
            Content = grid
        };
    }

    public static Border EmptyState(string title, string text)
    {
        return Card(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Title(title, 18),
                Caption(text)
            }
        });
    }

    public static Grid TwoColumns(View left, View right)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    public static Grid ActionRow(params View[] actions)
    {
        var grid = new Grid
        {
            ColumnSpacing = 10
        };

        for (var i = 0; i < actions.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(actions[i], i);
            grid.Children.Add(actions[i]);
        }

        return grid;
    }

    public static string Date(DateTime date) => date.ToString("dd.MM.yyyy");
}

public abstract class StudentContentPage : ContentPage
{
    protected StudentContentPage()
    {
        BackgroundColor = Ui.PageBackground;
        Shell.SetBackgroundColor(this, Ui.Surface);
        Shell.SetForegroundColor(this, Ui.Text);
        Shell.SetTitleColor(this, Ui.Text);
    }

    protected virtual bool AllowAnonymous => false;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!AllowAnonymous)
            await RequireStudentAsync();
    }

    protected async Task<bool> RequireStudentAsync()
    {
        var session = ServiceHelper.Get<AppSession>();
        await session.LoadAsync();

        if (session.IsAuthenticated &&
            string.Equals(session.Role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        session.SignOut();
        if (Shell.Current is AppShell shell)
            await shell.SignOutToLoginAsync();
        else
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");

        return false;
    }

    protected async Task RunBusyAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static ScrollView Scroll(View content)
    {
        return new ScrollView
        {
            BackgroundColor = Ui.PageBackground,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 14, 16, 20),
                Spacing = 14,
                Children = { content }
            }
        };
    }

    protected static Label ErrorLabel()
    {
        return new Label
        {
            TextColor = Ui.Danger,
            FontSize = 13,
            IsVisible = false,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    protected static void ShowError(Label label, string message)
    {
        label.Text = message;
        label.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    protected static string ErrorText<T>(Models.ApiResult<T> result, string fallback)
    {
        return string.IsNullOrWhiteSpace(result.ErrorMessage) ? fallback : result.ErrorMessage;
    }
}
