using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

[QueryProperty(nameof(AssignmentId), "assignmentId")]
public sealed class PracticeDetailsPage : StudentContentPage
{
    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly FileStorageService _files = ServiceHelper.Get<FileStorageService>();
    private readonly VerticalStackLayout _root = new() { Spacing = 12 };
    private readonly Picker _section = new()
    {
        Title = "Раздел",
        ItemsSource = new[] { "Сведения", "Организация", "Дневник", "Отчет", "Источники", "Приложения", "Документы" }
    };
    private readonly Label _message = ErrorLabel();
    private PracticeDetails? _practice;
    private int _assignmentId;

    public string AssignmentId
    {
        set => _assignmentId = int.TryParse(value, out var id) ? id : 0;
    }

    public PracticeDetailsPage()
    {
        Title = "Практика";
        _section.SelectedIndexChanged += (_, _) => RenderSelectedSection();
        Content = Scroll(_root);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await RequireStudentAsync())
            return;

        if (_assignmentId > 0)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _root.Children.Clear();
        _root.Children.Add(Ui.Title("Практика"));
        _root.Children.Add(_message);

        await RunBusyAsync(async () =>
        {
            var result = await _api.GetPracticeAsync(_assignmentId);
            if (!result.Success || result.Data is null)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить практику."));
                return;
            }

            _practice = result.Data;
            _section.SelectedIndex = _section.SelectedIndex < 0 ? 0 : _section.SelectedIndex;
            Render();
        });
    }

    private void Render()
    {
        _root.Children.Clear();
        if (_practice is null)
            return;

        _root.Children.Add(Ui.Title(_practice.Name, 22));
        _root.Children.Add(Ui.Caption($"{_practice.PracticeIndex} • {_practice.ProfessionalModuleCode} {_practice.ProfessionalModuleName}".Trim()));
        _root.Children.Add(Ui.Caption($"{Ui.Date(_practice.StartDate)} - {Ui.Date(_practice.EndDate)} • {_practice.Hours} ч."));
        _root.Children.Add(_section);
        _root.Children.Add(_message);
        RenderSelectedSection();
    }

    private void RenderSelectedSection()
    {
        while (_root.Children.Count > 5)
            _root.Children.RemoveAt(5);

        if (_practice is null)
            return;

        var selected = _section.SelectedItem?.ToString() ?? "Сведения";
        View section = selected switch
        {
            "Организация" => OrganizationSection(),
            "Дневник" => DiarySection(),
            "Отчет" => ReportSection(),
            "Источники" => SourcesSection(),
            "Приложения" => AppendicesSection(),
            "Документы" => DocumentsSection(),
            _ => OverviewSection()
        };

        _root.Children.Add(section);
    }

    private View OverviewSection()
    {
        var p = _practice!;
        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.Title("Сведения", 18),
                Ui.Caption($"Студент: {p.StudentFullName}"),
                Ui.Caption($"Группа: {p.StudentGroup}, курс {p.StudentCourse?.ToString() ?? "-"}"),
                Ui.Caption($"Специальность: {p.SpecialtyCode} {p.SpecialtyName}"),
                Ui.Caption($"Руководитель: {p.SupervisorFullName ?? "не назначен"}"),
                Ui.Caption($"Организация: {p.OrganizationFullName ?? p.OrganizationName ?? "не заполнена"}"),
                Ui.Caption($"ПК: {p.Competencies.Count}; ОК: {p.GeneralCompetencies.Count}; приложения: {p.Appendices.Count}")
            }
        });
    }

    private View OrganizationSection()
    {
        var p = _practice!;
        var orgName = Ui.Entry("Название организации");
        orgName.Text = p.OrganizationName;
        var fullName = Ui.Entry("Полное название");
        fullName.Text = p.OrganizationFullName;
        var shortName = Ui.Entry("Краткое название");
        shortName.Text = p.OrganizationShortName;
        var address = Ui.Editor("Адрес", 2);
        address.Text = p.OrganizationAddress;
        var supervisor = Ui.Entry("ФИО руководителя");
        supervisor.Text = p.OrganizationSupervisorFullName;
        var position = Ui.Entry("Должность руководителя");
        position.Text = p.OrganizationSupervisorPosition;
        var phone = Ui.Entry("Телефон", Keyboard.Telephone);
        phone.Text = p.OrganizationSupervisorPhone;
        var email = Ui.Entry("Email руководителя", Keyboard.Email);
        email.Text = p.OrganizationSupervisorEmail;
        var goal = Ui.Editor("Цель практики", 3);
        goal.Text = p.IntroductionMainGoal;
        var task = Ui.Editor("Задание на практику", 4);
        task.Text = p.PracticeTaskContent;
        var duties = Ui.Editor("Выполняемые обязанности", 4);
        duties.Text = p.StudentDuties;
        var materials = Ui.Editor("Предоставленные материалы", 3);
        materials.Text = p.ProvidedMaterialsDescription;
        var schedule = Ui.Editor("График работы", 3);
        schedule.Text = p.WorkScheduleDescription;

        var save = Ui.PrimaryButton("Сохранить сведения");
        save.Clicked += async (_, _) =>
        {
            p.OrganizationName = orgName.Text;
            p.OrganizationFullName = fullName.Text;
            p.OrganizationShortName = shortName.Text;
            p.OrganizationAddress = address.Text;
            p.OrganizationSupervisorFullName = supervisor.Text;
            p.OrganizationSupervisorPosition = position.Text;
            p.OrganizationSupervisorPhone = phone.Text;
            p.OrganizationSupervisorEmail = email.Text;
            p.IntroductionMainGoal = goal.Text;
            p.PracticeTaskContent = task.Text;
            p.StudentDuties = duties.Text;
            p.ProvidedMaterialsDescription = materials.Text;
            p.WorkScheduleDescription = schedule.Text;
            await SavePracticeResultAsync(() => _api.SaveOrganizationAsync(_assignmentId, p));
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title("Организация и задание", 18),
                orgName, fullName, shortName, address, supervisor, position, phone, email,
                goal, task, duties, materials, schedule, save
            }
        });
    }

    private View DiarySection()
    {
        var p = _practice!;
        var dates = GetWorkDates(p).ToList();
        var picker = new Picker { Title = "Дата", ItemsSource = dates.Select(Ui.Date).ToList() };
        var shortText = Ui.Editor("Краткая запись в дневник", 4);
        var detailedText = Ui.Editor("Подробный отчет за день", 8);
        var reviewed = Ui.Caption("");

        void LoadEntry()
        {
            if (picker.SelectedIndex < 0)
                return;

            var date = dates[picker.SelectedIndex].Date;
            var entry = p.DiaryEntries.FirstOrDefault(x => x.WorkDate.Date == date) ?? new DiaryEntry { WorkDate = date };
            shortText.Text = entry.ShortDescription;
            detailedText.Text = entry.DetailedReport;
            reviewed.Text = entry.IsReviewed
                ? $"Проверено: оценка {entry.SupervisorGrade?.ToString() ?? "-"}, {entry.SupervisorComment}"
                : "Пока не проверено руководителем.";
        }

        picker.SelectedIndexChanged += (_, _) => LoadEntry();
        picker.SelectedIndex = dates.Count == 0 ? -1 : 0;

        var save = Ui.PrimaryButton("Сохранить день");
        save.Clicked += async (_, _) =>
        {
            if (picker.SelectedIndex < 0)
                return;

            var date = dates[picker.SelectedIndex].Date;
            var entry = p.DiaryEntries.FirstOrDefault(x => x.WorkDate.Date == date) ?? new DiaryEntry { WorkDate = date };
            entry.ShortDescription = shortText.Text ?? string.Empty;
            entry.DetailedReport = detailedText.Text ?? string.Empty;
            await SavePracticeResultAsync(() => _api.SaveDiaryEntryAsync(_assignmentId, entry));
        };

        var attach = Ui.SecondaryButton("Прикрепить изображение");
        attach.Clicked += async (_, _) =>
        {
            if (picker.SelectedIndex < 0)
                return;

            var file = await FilePicker.Default.PickAsync(PickOptions.Images);
            if (file is null)
                return;

            var result = await _api.UploadDiaryAttachmentAsync(_assignmentId, dates[picker.SelectedIndex], file.FileName, file);
            if (!result.Success)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить изображение."));
                return;
            }

            await LoadAsync();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title("Дневник", 18),
                Ui.Caption("Краткая запись идет в дневник, подробный отчет используется для итогового отчета."),
                picker,
                reviewed,
                shortText,
                detailedText,
                save,
                attach
            }
        });
    }

    private View ReportSection()
    {
        var p = _practice!;
        var items = Ui.Editor("Элементы отчета: Категория | Название | Описание", 12);
        items.Text = string.Join(Environment.NewLine, p.ReportItems.Select(x => $"{x.Category} | {x.Name} | {x.Description}"));
        var save = Ui.PrimaryButton("Сохранить таблицы отчета");
        save.Clicked += async (_, _) =>
        {
            var parsed = items.Text?
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line =>
                {
                    var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
                    return new ReportItem
                    {
                        Category = parts.ElementAtOrDefault(0) ?? "Custom",
                        Name = parts.ElementAtOrDefault(1) ?? parts.ElementAtOrDefault(0) ?? string.Empty,
                        Description = parts.ElementAtOrDefault(2)
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList() ?? new List<ReportItem>();

            await SavePracticeResultAsync(() => _api.SaveReportItemsAsync(_assignmentId, parsed));
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title("Отчет", 18),
                Ui.Caption("Формат строки: категория | название | описание. Категории веб-версии сохраняются как есть."),
                items,
                save
            }
        });
    }

    private View SourcesSection()
    {
        var p = _practice!;
        var sources = Ui.Editor("Источники: Название | URL | Описание", 10);
        sources.Text = string.Join(Environment.NewLine, p.Sources.Select(x => $"{x.Title} | {x.Url} | {x.Description}"));
        var save = Ui.PrimaryButton("Сохранить источники");
        save.Clicked += async (_, _) =>
        {
            var parsed = sources.Text?
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line =>
                {
                    var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
                    return new PracticeSource
                    {
                        Title = parts.ElementAtOrDefault(0) ?? string.Empty,
                        Url = parts.ElementAtOrDefault(1),
                        Description = parts.ElementAtOrDefault(2)
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Title))
                .ToList() ?? new List<PracticeSource>();

            await SavePracticeResultAsync(() => _api.SaveSourcesAsync(_assignmentId, parsed));
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title("Источники", 18),
                sources,
                save
            }
        });
    }

    private View AppendicesSection()
    {
        var p = _practice!;
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(Ui.Title("Приложения", 18));

        foreach (var appendix in p.Appendices.OrderByDescending(x => x.CreatedAtUtc))
        {
            var open = Ui.SecondaryButton("Открыть");
            open.Clicked += async (_, _) => await DownloadAndOpenApiFileAsync($"api/Student/appendices/{appendix.Id}/download");
            var delete = Ui.SecondaryButton("Удалить");
            delete.Clicked += async (_, _) =>
            {
                var result = await _api.DeleteAppendixAsync(appendix.Id);
                if (!result.Success)
                    ShowError(_message, ErrorText(result, "Не удалось удалить приложение."));
                else
                    await LoadAsync();
            };

            stack.Children.Add(Ui.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Ui.Title(appendix.Title, 16),
                    Ui.Caption($"{appendix.FileName} • {appendix.SizeBytes / 1024} КБ"),
                    open,
                    delete
                }
            }));
        }

        var title = Ui.Entry("Название");
        var description = Ui.Editor("Описание", 2);
        var upload = Ui.PrimaryButton("Прикрепить файл");
        upload.Clicked += async (_, _) =>
        {
            var file = await FilePicker.Default.PickAsync();
            if (file is null)
                return;

            var result = await _api.UploadAppendixAsync(_assignmentId, file, title.Text, description.Text);
            if (!result.Success)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить приложение."));
                return;
            }

            await LoadAsync();
        };

        stack.Children.Add(title);
        stack.Children.Add(description);
        stack.Children.Add(upload);
        return Ui.Card(stack);
    }

    private View DocumentsSection()
    {
        var p = _practice!;
        var diaryDocx = Ui.PrimaryButton("Дневник DOCX");
        diaryDocx.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadPracticeDiary?assignmentId={p.AssignmentId}");
        var diaryPdf = Ui.SecondaryButton("Дневник PDF");
        diaryPdf.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadPracticeDiaryPdf?assignmentId={p.AssignmentId}");
        var reportDocx = Ui.PrimaryButton("Отчет DOCX");
        reportDocx.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadPracticeReport?assignmentId={p.AssignmentId}");
        var reportPdf = Ui.SecondaryButton("Отчет PDF");
        reportPdf.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadPracticeReportPdf?assignmentId={p.AssignmentId}");
        var attestDocx = Ui.PrimaryButton("Аттестационный DOCX");
        attestDocx.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadAttestation?assignmentId={p.AssignmentId}");
        var attestPdf = Ui.SecondaryButton("Аттестационный PDF");
        attestPdf.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync($"Student/DownloadAttestationPdf?assignmentId={p.AssignmentId}");

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title("Документы", 18),
                Ui.Caption("Документы формируются веб-сервисом, но мобильное приложение передает тот же JWT. URL веба меняется в настройках."),
                diaryDocx,
                diaryPdf,
                reportDocx,
                reportPdf,
                attestDocx,
                attestPdf
            }
        });
    }

    private async Task SavePracticeResultAsync(Func<Task<ApiResult<PracticeDetails>>> save)
    {
        var result = await save();
        if (!result.Success || result.Data is null)
        {
            ShowError(_message, ErrorText(result, "Не удалось сохранить данные."));
            return;
        }

        _practice = result.Data;
        ShowError(_message, string.Empty);
        Render();
    }

    private async Task DownloadAndOpenApiFileAsync(string url)
    {
        var result = await _api.DownloadApiFileAsync(url);
        await OpenFileResultAsync(result);
    }

    private async Task DownloadAndOpenWebFileAsync(string url)
    {
        var result = await _api.DownloadWebDocumentAsync(url);
        await OpenFileResultAsync(result);
    }

    private async Task OpenFileResultAsync(ApiResult<FileDownload> result)
    {
        if (!result.Success || result.Data is null)
        {
            ShowError(_message, ErrorText(result, "Не удалось открыть файл."));
            return;
        }

        await _files.OpenAsync(result.Data);
    }

    private static IEnumerable<DateTime> GetWorkDates(PracticeDetails practice)
    {
        for (var date = practice.StartDate.Date; date <= practice.EndDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                yield return date;
        }
    }
}
