using Microsoft.Maui.Controls.Shapes;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

public sealed class ProfilePage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly AppSession _session = ServiceHelper.Get<AppSession>();
    private readonly PushRegistrationService _pushRegistration = ServiceHelper.Get<PushRegistrationService>();
    private readonly Entry _surname = Ui.Entry("Фамилия");
    private readonly Entry _name = Ui.Entry("Имя");
    private readonly Entry _patronymic = Ui.Entry("Отчество");
    private readonly Entry _email = Ui.Entry("Email", Keyboard.Email);
    private readonly Entry _avatar = Ui.Entry("Ссылка на аватар");
    private readonly Label _info = ErrorLabel();
    private readonly VerticalStackLayout _root = new() { Spacing = 14 };
    private CurrentUser? _user;
    private bool _editing;

    public ProfilePage()
    {
        Title = "Профиль";
        Content = Scroll(_root);
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
        var result = await _api.GetCurrentUserAsync();
        if (!result.Success || result.Data is null)
        {
            _info.TextColor = Ui.Danger;
            ShowError(_info, ErrorText(result, "Не удалось загрузить профиль."));
            Render();
            return;
        }

        _user = result.Data;
        _session.CurrentUser = result.Data;
        FillForm(result.Data);
        ShowError(_info, string.Empty);
        Render();
    }

    private void Render()
    {
        DetachReusableViews();
        _root.Children.Clear();
        var displayName = _user?.FullName ?? _session.FullName ?? "Студент";

        _root.Children.Add(Ui.Hero("Профиль", displayName, AvatarPreview(_user)));
        _root.Children.Add(_info);
        _root.Children.Add(_editing ? EditCard() : ReadonlyCard());
        _root.Children.Add(PushCard());
        _root.Children.Add(AccessCard());
        _root.Children.Add(SessionCard());
    }

    private View ReadonlyCard()
    {
        var edit = Ui.PrimaryButton("Редактировать");
        edit.Clicked += (_, _) =>
        {
            _editing = true;
            if (_user is not null)
                FillForm(_user);
            ShowError(_info, string.Empty);
            Render();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Eyebrow("Данные аккаунта"),
                ReadonlyItem("Фамилия", _user?.Surname),
                ReadonlyItem("Имя", _user?.FirstName),
                ReadonlyItem("Отчество", _user?.Patronymic),
                ReadonlyItem("Email", _user?.Email),
                ReadonlyItem("Группа", _user?.GroupName),
                ReadonlyItem("Специальность", $"{_user?.SpecialtyCode} {_user?.SpecialtyName}".Trim()),
                edit
            }
        });
    }

    private View EditCard()
    {
        DetachProfileInputs();

        var save = Ui.PrimaryButton("Сохранить");
        save.Clicked += async (_, _) => await SaveAsync();

        var cancel = Ui.SecondaryButton("Отменить");
        cancel.Clicked += (_, _) =>
        {
            _editing = false;
            if (_user is not null)
                FillForm(_user);
            ShowError(_info, string.Empty);
            Render();
        };

        var clearAvatar = Ui.SecondaryButton("Убрать аватар");
        clearAvatar.Clicked += (_, _) => _avatar.Text = string.Empty;

        var chooseAvatar = Ui.SecondaryButton("Выбрать аватар");
        chooseAvatar.Clicked += async (_, _) => await SelectAvatarAsync();

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Eyebrow("Редактирование профиля"),
                Notice("Изменение ФИО или email влияет на данные аккаунта. Перед сохранением приложение попросит подтверждение.", Ui.Warning),
                _surname,
                _name,
                _patronymic,
                _email,
                _avatar,
                Ui.ActionRow(chooseAvatar, clearAvatar),
                Ui.ActionRow(cancel, save)
            }
        });
    }

    private View AccessCard()
    {
        var password = Ui.SecondaryButton("Изменить пароль");
        password.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(ChangePasswordPage));

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Eyebrow("Восстановление доступа"),
                Ui.Caption("Здесь можно сменить пароль для входа в мобильный и веб-кабинет."),
                password
            }
        });
    }

    private View PushCard()
    {
        var toggle = new Switch
        {
            IsToggled = _pushRegistration.IsEnabled,
            OnColor = Ui.Primary,
            ThumbColor = Colors.White
        };

        var status = Ui.Caption(_pushRegistration.IsEnabled
            ? "Push-уведомления включены для новых сообщений и системных уведомлений."
            : "Push-уведомления выключены на этом устройстве.");

        toggle.Toggled += async (_, e) =>
        {
            if (e.Value)
            {
                _pushRegistration.IsEnabled = true;
                var registered = await _pushRegistration.EnsureRegisteredAsync();
                _info.TextColor = registered ? Ui.Accent : Ui.Warning;
                ShowError(_info, registered
                    ? "Push-уведомления включены."
                    : "Не удалось зарегистрировать устройство. Проверьте разрешение уведомлений и Firebase-конфиг приложения.");
            }
            else
            {
                await _pushRegistration.DisableAsync();
                _info.TextColor = Ui.Accent;
                ShowError(_info, "Push-уведомления выключены.");
            }

            Render();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Eyebrow("Уведомления"),
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        toggle,
                        new Label
                        {
                            Text = "Push-уведомления",
                            TextColor = Ui.Text,
                            FontSize = 15,
                            FontAttributes = FontAttributes.Bold,
                            VerticalTextAlignment = TextAlignment.Center
                        }
                    }
                },
                status
            }
        });
    }

    private View SessionCard()
    {
        var logout = Ui.SecondaryButton("Выйти");
        logout.TextColor = Ui.Danger;
        logout.Clicked += async (_, _) =>
        {
            if (Shell.Current is AppShell shell)
                await shell.SignOutToLoginAsync();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Eyebrow("Сессия"),
                logout
            }
        });
    }

    private void FillForm(CurrentUser user)
    {
        _surname.Text = user.Surname;
        _name.Text = user.FirstName;
        _patronymic.Text = user.Patronymic;
        _email.Text = user.Email;
        _avatar.Text = user.AvatarUrl;
    }

    private void DetachReusableViews()
    {
        Ui.Detach(_info);
        DetachProfileInputs();
    }

    private void DetachProfileInputs()
    {
        Ui.Detach(_surname);
        Ui.Detach(_name);
        Ui.Detach(_patronymic);
        Ui.Detach(_email);
        Ui.Detach(_avatar);
    }

    private async Task SaveAsync()
    {
        if (_user is null)
            return;

        if (HasImportantChanges(_user))
        {
            var confirmed = await DisplayAlert(
                "Подтверждение изменений",
                "ФИО и email используются в документах, уведомлениях и профиле. Сохранить изменения?",
                "Сохранить",
                "Отмена");

            if (!confirmed)
                return;
        }

        _user.Surname = _surname.Text ?? string.Empty;
        _user.FirstName = _name.Text ?? string.Empty;
        _user.Patronymic = string.IsNullOrWhiteSpace(_patronymic.Text) ? null : _patronymic.Text;
        _user.Email = _email.Text ?? string.Empty;
        _user.AvatarUrl = string.IsNullOrWhiteSpace(_avatar.Text) ? null : _avatar.Text;
        _user.Theme = "dark";

        var result = await _api.UpdateProfileAsync(_user);
        if (!result.Success || result.Data is null)
        {
            _info.TextColor = Ui.Danger;
            ShowError(_info, ErrorText(result, "Не удалось сохранить профиль."));
            Render();
            return;
        }

        _user = result.Data;
        _session.CurrentUser = result.Data;
        _editing = false;
        FillForm(result.Data);
        _info.TextColor = Ui.Accent;
        ShowError(_info, "Профиль сохранён.");
        Render();
    }

    private async Task SelectAvatarAsync()
    {
        var file = await FilePicker.Default.PickAsync(PickOptions.Images);
        if (file is null)
            return;

        var result = await _api.UploadAvatarAsync(file);
        if (!result.Success || result.Data is null)
        {
            _info.TextColor = Ui.Danger;
            ShowError(_info, ErrorText(result, "Не удалось загрузить аватар."));
            Render();
            return;
        }

        if (_user is not null)
            _user.AvatarUrl = result.Data.AvatarUrl;

        _avatar.Text = result.Data.AvatarUrl;
        _session.CurrentUser = result.Data;
        _info.TextColor = Ui.Accent;
        ShowError(_info, "Аватар обновлён.");
        Render();
    }

    private bool HasImportantChanges(CurrentUser user)
    {
        return !string.Equals(user.Surname ?? string.Empty, _surname.Text ?? string.Empty, StringComparison.Ordinal) ||
               !string.Equals(user.FirstName ?? string.Empty, _name.Text ?? string.Empty, StringComparison.Ordinal) ||
               !string.Equals(user.Patronymic ?? string.Empty, _patronymic.Text ?? string.Empty, StringComparison.Ordinal) ||
               !string.Equals(user.Email ?? string.Empty, _email.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private View AvatarPreview(CurrentUser? user)
    {
        var url = ResolveAvatarUrl(user?.AvatarUrl);
        View content = string.IsNullOrWhiteSpace(url)
            ? new Label
            {
                Text = Initials(user?.FullName ?? _session.FullName ?? "PM"),
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
            : new Image
            {
                Source = ImageSource.FromUri(new Uri(url)),
                Aspect = Aspect.AspectFill
            };

        return new Border
        {
            WidthRequest = 64,
            HeightRequest = 64,
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#7EA0FF"),
            StrokeShape = new RoundRectangle { CornerRadius = 32 },
            BackgroundColor = Ui.Primary,
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

    private static View ReadonlyItem(string label, string? value)
    {
        return new Border
        {
            BackgroundColor = Ui.SurfaceInput,
            Stroke = Ui.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    Ui.Eyebrow(label),
                    new Label
                    {
                        Text = string.IsNullOrWhiteSpace(value) ? "Не указано" : value,
                        TextColor = string.IsNullOrWhiteSpace(value) ? Ui.Faint : Ui.Text,
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }

    private static View Notice(string text, Color color)
    {
        return new Border
        {
            BackgroundColor = Color.FromRgba(color.Red, color.Green, color.Blue, 0.13),
            Stroke = Color.FromRgba(color.Red, color.Green, color.Blue, 0.28),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12),
            Content = new Label
            {
                Text = text,
                TextColor = color,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
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
}
