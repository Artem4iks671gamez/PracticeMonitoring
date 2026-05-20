using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Maui.Controls.Shapes;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Models;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile.Pages;

[QueryProperty(nameof(AssignmentId), "assignmentId")]
public sealed class PracticeDetailsPage : StudentContentPage
{
    private const string OverviewTab = "overview";
    private const string OrganizationTab = "organization";
    private const string DiaryTab = "diary";
    private const string IntroductionTab = "introduction";
    private const string TechnicalTab = "technical";
    private const string SourcesTab = "sources";
    private const string AppendicesTab = "appendices";
    private const string DocumentsTab = "documents";

    private static readonly PracticeTab[] Tabs =
    {
        new(OverviewTab, "Сведения"),
        new(OrganizationTab, "Организация"),
        new(DiaryTab, "Дневник"),
        new(IntroductionTab, "Введение"),
        new(TechnicalTab, "Тех. средства"),
        new(SourcesTab, "Источники"),
        new(AppendicesTab, "Приложения"),
        new(DocumentsTab, "Документы")
    };

    private static readonly string[] TechnicalCharacteristics =
    {
        "Размер экрана",
        "Разрешение экрана",
        "Процессор",
        "Количество ядер процессора",
        "Оперативная память",
        "Тип видеокарты",
        "Видеокарта",
        "Конфигурация накопителей",
        "Общий объем всех накопителей",
        "Операционная система"
    };

    private readonly ApiClient _api = ServiceHelper.Get<ApiClient>();
    private readonly FileStorageService _files = ServiceHelper.Get<FileStorageService>();
    private readonly VerticalStackLayout _root = new() { Spacing = 14 };
    private HorizontalStackLayout _tabs = new() { Spacing = 8 };
    private ContentView _sectionHost = new();
    private readonly Label _message = ErrorLabel();
    private readonly List<SourceDraft> _sourceDrafts = new();
    private readonly List<SourceEditorRow> _sourceEditors = new();
    private readonly Dictionary<DateTime, DiaryDayDraft> _diaryDrafts = new();

    private PracticeDetails? _practice;
    private int _assignmentId;
    private string _selectedTab = OverviewTab;
    private DateTime? _selectedDiaryDate;
    private bool _organizationEditing;
    private bool _introductionEditing;
    private bool _technicalEditing;
    private bool _sourcesEditing;

    public string AssignmentId
    {
        set => _assignmentId = int.TryParse(value, out var id) ? id : 0;
    }

    public PracticeDetailsPage()
    {
        Title = "Практика";
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
        _root.Children.Add(Ui.Hero("Практика", "Загрузка сведений по назначенной производственной практике."));
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
            _diaryDrafts.Clear();
            _selectedDiaryDate ??= GetDefaultDiaryDate(result.Data);
            RenderAll();
        });
    }

    private void RenderAll()
    {
        _root.Children.Clear();
        if (_practice is null)
            return;

        _root.Children.Add(Ui.Hero(
            $"{FormatPracticeIndex(_practice.PracticeIndex)} - {_practice.Name}",
            $"{FormatDateRange(_practice)} · {FormatProfessionalModuleCode(_practice.ProfessionalModuleCode)} {_practice.ProfessionalModuleName}".Trim()));

        _root.Children.Add(BuildStatusStrip(_practice));
        _tabs = new HorizontalStackLayout { Spacing = 8 };
        _sectionHost = new ContentView();
        RenderTabs();
        _root.Children.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = _tabs
        });
        _root.Children.Add(_message);
        _root.Children.Add(_sectionHost);
        RenderSelectedSection();
    }

    private View BuildStatusStrip(PracticeDetails practice)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        AddGrid(grid, Ui.Metric("Дневник", $"{practice.DiaryEntriesCount}/{practice.WorkDaysCount}"), 0, 0);
        AddGrid(grid, Ui.Metric("Часы", $"{practice.Hours}"), 1, 0);
        AddGrid(grid, Ui.Metric("Сведения", practice.HasRequiredDetails ? "Готово" : "Нужно заполнить"), 0, 1);
        AddGrid(grid, Ui.Metric("Статус", practice.IsCompleted ? "Завершена" : "Активна"), 1, 1);

        return grid;
    }

    private void RenderTabs()
    {
        _tabs.Children.Clear();
        foreach (var tab in Tabs)
        {
            var active = tab.Key == _selectedTab;
            var button = new Button
            {
                Text = tab.Title,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 42,
                Padding = new Thickness(14, 0),
                BackgroundColor = active ? Ui.Primary : Ui.SurfaceRaised,
                TextColor = active ? Colors.White : Ui.PrimarySoft,
                BorderColor = active ? Ui.Primary : Ui.Border,
                BorderWidth = 1
            };

            button.Clicked += (_, _) =>
            {
                _selectedTab = tab.Key;
                ShowError(_message, string.Empty);
                RenderTabs();
                RenderSelectedSection();
            };

            _tabs.Children.Add(button);
        }
    }

    private void RenderSelectedSection()
    {
        if (_practice is null)
            return;

        _sectionHost.Content = _selectedTab switch
        {
            OrganizationTab => OrganizationSection(),
            DiaryTab => DiarySection(),
            IntroductionTab => IntroductionSection(),
            TechnicalTab => TechnicalSection(),
            SourcesTab => SourcesSection(),
            AppendicesTab => AppendicesSection(),
            DocumentsTab => DocumentsSection(),
            _ => OverviewSection()
        };
    }

    private View OverviewSection()
    {
        var p = _practice!;
        var stack = new VerticalStackLayout { Spacing = 14 };

        stack.Children.Add(SectionCard("Основная информация", ReadOnlyGrid(new[]
        {
            Row("Практика", $"{FormatPracticeIndex(p.PracticeIndex)} {p.Name}"),
            Row("Период", FormatDateRange(p)),
            Row("Специальность", $"{p.SpecialtyCode} {p.SpecialtyName}".Trim()),
            Row("Квалификация", p.QualificationName),
            Row("Профессиональный модуль", $"{FormatProfessionalModuleCode(p.ProfessionalModuleCode)} {p.ProfessionalModuleName}".Trim()),
            Row("Руководитель от техникума", p.SupervisorFullName),
            Row("Студент", p.StudentFullName),
            Row("Группа", $"{p.StudentGroup}{(p.StudentCourse.HasValue ? $", {p.StudentCourse} курс" : string.Empty)}"),
            Row("Срок заполнения сведений", Ui.Date(p.DetailsDueDate)),
            Row("Организация", FirstNotEmpty(p.OrganizationFullName, p.OrganizationName))
        })));

        stack.Children.Add(CompetencySection("Общие компетенции", p.GeneralCompetencies
            .OrderBy(x => x.SortOrder)
            .Select(x => ($"{x.CompetencyCode}", x.CompetencyDescription, string.Empty))));

        stack.Children.Add(CompetencySection("Профессиональные компетенции и виды работ", p.Competencies
            .Select(x => ($"{x.CompetencyCode} · {x.Hours} ч.", x.CompetencyDescription, x.WorkTypes))));

        stack.Children.Add(CommentsFor("overview"));
        return stack;
    }

    private View OrganizationSection()
    {
        return _organizationEditing ? OrganizationEditSection() : OrganizationReadonlySection();
    }

    private View OrganizationReadonlySection()
    {
        var p = _practice!;
        var edit = Ui.PrimaryButton("Редактировать");
        edit.Clicked += (_, _) =>
        {
            _organizationEditing = true;
            ShowError(_message, string.Empty);
            RenderSelectedSection();
        };

        var content = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                HeaderWithAction("Сведения об организации", edit),
                CommentsFor("organization"),
                ReadOnlyGrid(new[]
                {
                    Row("Полное название", FirstNotEmpty(p.OrganizationFullName, p.OrganizationName)),
                    Row("Краткое название", p.OrganizationShortName),
                    Row("Адрес", p.OrganizationAddress),
                    Row("Руководитель", p.OrganizationSupervisorFullName),
                    Row("Должность руководителя", p.OrganizationSupervisorPosition),
                    Row("Телефон", p.OrganizationSupervisorPhone),
                    Row("Почта", p.OrganizationSupervisorEmail),
                    Row("Содержание задания", p.PracticeTaskContent)
                })
            }
        };

        if (!p.HasRequiredDetails)
            content.Children.Insert(1, Notice("Основные сведения ещё не заполнены. Нажмите «Редактировать», заполните поля и сохраните изменения.", Ui.Warning));

        return Ui.Card(content);
    }

    private View OrganizationEditSection()
    {
        var p = _practice!;
        var fullName = LabeledEntry("Полное название организации", FirstNotEmpty(p.OrganizationFullName, p.OrganizationName));
        var shortName = LabeledEntry("Сокращенное название", p.OrganizationShortName);
        var address = LabeledEditor("Адрес организации", p.OrganizationAddress, 3);
        var supervisor = LabeledEntry("ФИО руководителя от организации", p.OrganizationSupervisorFullName);
        var position = LabeledEntry("Должность руководителя", p.OrganizationSupervisorPosition);
        var phone = LabeledEntry("Телефон", p.OrganizationSupervisorPhone, Keyboard.Telephone);
        var email = LabeledEntry("Почта руководителя", p.OrganizationSupervisorEmail, Keyboard.Email);
        var task = LabeledEditor("Содержание задания", p.PracticeTaskContent, 5);

        var save = Ui.PrimaryButton("Сохранить изменения");
        var cancel = Ui.SecondaryButton("Отменить");
        cancel.Clicked += (_, _) =>
        {
            _organizationEditing = false;
            ShowError(_message, string.Empty);
            RenderSelectedSection();
        };

        save.Clicked += async (_, _) =>
        {
            p.OrganizationName = fullName.Input.Text;
            p.OrganizationFullName = fullName.Input.Text;
            p.OrganizationShortName = shortName.Input.Text;
            p.OrganizationAddress = address.Input.Text;
            p.OrganizationSupervisorFullName = supervisor.Input.Text;
            p.OrganizationSupervisorPosition = position.Input.Text;
            p.OrganizationSupervisorPhone = phone.Input.Text;
            p.OrganizationSupervisorEmail = email.Input.Text;
            p.PracticeTaskContent = task.Input.Text;
            _organizationEditing = false;
            await SavePracticeResultAsync(() => _api.SaveOrganizationAsync(_assignmentId, p));
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Title("Редактирование организации", 18),
                Notice("После сохранения руководитель практики увидит изменения. Эти сведения попадают в документы.", Ui.PrimarySoft),
                fullName.View, shortName.View, address.View, supervisor.View, position.View, phone.View, email.View, task.View,
                Ui.ActionRow(cancel, save)
            }
        });
    }

    private View DiarySection()
    {
        var p = _practice!;
        var dates = GetWorkDates(p).ToList();
        if (dates.Count == 0)
            return Ui.EmptyState("Дневник недоступен", "В периоде практики нет рабочих дней.");

        _selectedDiaryDate ??= dates[0];
        if (!dates.Any(x => x.Date == _selectedDiaryDate.Value.Date))
            _selectedDiaryDate = dates[0];

        var selectedDate = _selectedDiaryDate.Value.Date;
        var entry = p.DiaryEntries.FirstOrDefault(x => x.WorkDate.Date == selectedDate) ?? new DiaryEntry { WorkDate = selectedDate };
        var days = new HorizontalStackLayout { Spacing = 8 };
        var entriesByDate = p.DiaryEntries.ToDictionary(x => x.WorkDate.Date, x => x);

        foreach (var date in dates)
        {
            entriesByDate.TryGetValue(date.Date, out var dayEntry);
            var active = date.Date == selectedDate;
            var filled = dayEntry is not null && !string.IsNullOrWhiteSpace(dayEntry.ShortDescription);
            var reviewed = dayEntry?.IsReviewed == true;
            var button = new Button
            {
                Text = $"{date:dd.MM}\n{ShortWeekday(date)}\n{(reviewed ? "проверено" : filled ? "заполнено" : "пусто")}",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                WidthRequest = 96,
                HeightRequest = 76,
                BackgroundColor = active ? Ui.Primary : filled ? Ui.SurfaceRaised : Ui.SurfaceInput,
                TextColor = active ? Colors.White : filled ? Ui.PrimarySoft : Ui.Muted,
                BorderColor = reviewed ? Ui.Accent : Ui.Border,
                BorderWidth = 1
            };
            button.Clicked += (_, _) =>
            {
                _selectedDiaryDate = date.Date;
                ShowError(_message, string.Empty);
                RenderSelectedSection();
            };
            days.Children.Add(button);
        }

        var shortText = LabeledEditor("Краткое описание для дневника", entry.ShortDescription, 5);

        var save = Ui.PrimaryButton("Сохранить день");
        save.Clicked += async (_, _) =>
        {
            entry.WorkDate = selectedDate;
            entry.ShortDescription = shortText.Input.Text?.Trim() ?? string.Empty;
            await SavePracticeResultAsync(() => _api.SaveDiarySummaryAsync(_assignmentId, entry.WorkDate, entry.ShortDescription));
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                SectionCard("Рабочие дни", new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                    Content = days
                }),
                SectionCard($"Запись за {Ui.Date(selectedDate)}", new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        DiaryReview(entry),
                        shortText.View,
                        Notice("Основной подробный отчёт за день с таблицами и изображениями заполняется в Desktop-версии веб-приложения. Мобильное приложение сохраняет только краткую запись дневника, чтобы не портить структуру отчёта.", Ui.PrimarySoft),
                        save
                    }
                })
            }
        };

        return stack;
    }

    private DiaryDayDraft GetDiaryDraft(DateTime date, DiaryEntry entry)
    {
        if (_diaryDrafts.TryGetValue(date.Date, out var draft))
            return draft;

        draft = new DiaryDayDraft
        {
            ShortDescription = entry.ShortDescription ?? string.Empty,
            Report = ParseReportDocumentDraft(entry.DetailedReport, entry.Attachments)
        };
        _diaryDrafts[date.Date] = draft;
        return draft;
    }

    private View ReportEditorView(DayReportDraft report, DiaryEntry entry)
    {
        if (report.Blocks.Count == 0)
            report.Blocks.Add(CreateTextReportBlock());

        var addText = SmallReportButton("Текст");
        addText.Clicked += (_, _) =>
        {
            report.Blocks.Add(CreateTextReportBlock());
            RenderSelectedSection();
        };

        var addTable = SmallReportButton("Таблица");
        addTable.Clicked += (_, _) =>
        {
            report.Blocks.Add(CreateTableReportBlock());
            RenderSelectedSection();
        };

        var addImage = SmallReportButton("Рисунок");
        addImage.Clicked += async (_, _) => await AddImageReportBlockAsync(report);

        var stack = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Eyebrow("Подробный отчёт за день"),
                WrapActions(addText, addTable, addImage)
            }
        };

        for (var i = 0; i < report.Blocks.Count; i++)
            stack.Children.Add(ReportBlockEditor(report, entry, report.Blocks[i], i));

        return stack;
    }

    private View ReportBlockEditor(DayReportDraft report, DiaryEntry entry, ReportBlockDraft block, int index)
    {
        var up = SmallReportButton("Выше");
        up.IsEnabled = index > 0;
        up.Clicked += (_, _) =>
        {
            MoveReportBlock(report, index, -1);
            RenderSelectedSection();
        };

        var down = SmallReportButton("Ниже");
        down.IsEnabled = index < report.Blocks.Count - 1;
        down.Clicked += (_, _) =>
        {
            MoveReportBlock(report, index, 1);
            RenderSelectedSection();
        };

        var delete = SmallReportButton("Удалить");
        delete.TextColor = Ui.Danger;
        delete.Clicked += (_, _) =>
        {
            report.Blocks.Remove(block);
            if (report.Blocks.Count == 0)
                report.Blocks.Add(CreateTextReportBlock());
            RenderSelectedSection();
        };

        var content = block switch
        {
            TextReportBlockDraft textBlock => TextReportBlockEditor(textBlock),
            TableReportBlockDraft tableBlock => TableReportBlockEditor(tableBlock),
            ImageReportBlockDraft imageBlock => ImageReportBlockEditor(imageBlock, entry),
            _ => Ui.Caption("Неизвестный блок отчёта.")
        };

        var header = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.Title($"{index + 1}. {ReportBlockTitle(block)}", 15),
                WrapActions(up, down, delete)
            }
        };

        return Ui.Panel(new VerticalStackLayout
        {
            Spacing = 12,
            Children = { header, content }
        });
    }

    private View TextReportBlockEditor(TextReportBlockDraft block)
    {
        var mode = Ui.Picker("Тип текста");
        mode.Items.Add("Обычный текст");
        mode.Items.Add("Подзаголовок");
        mode.SelectedIndex = string.Equals(block.Mode, "heading", StringComparison.Ordinal) ? 1 : 0;
        mode.SelectedIndexChanged += (_, _) => block.Mode = mode.SelectedIndex == 1 ? "heading" : "paragraph";

        var editor = Ui.Editor("Текст блока", 6);
        editor.Text = block.Content;
        editor.TextChanged += (_, e) => block.Content = e.NewTextValue ?? string.Empty;

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow("Тип"), mode } },
                new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow("Текст"), editor } }
            }
        };
    }

    private View TableReportBlockEditor(TableReportBlockDraft block)
    {
        EnsureTableShape(block);

        var title = Ui.Entry("Название таблицы");
        title.Text = block.Title;
        title.TextChanged += (_, e) => block.Title = e.NewTextValue ?? string.Empty;

        var headerSwitch = new Switch
        {
            IsToggled = block.HasHeaderRow,
            OnColor = Ui.Primary,
            ThumbColor = Colors.White
        };
        headerSwitch.Toggled += (_, e) => block.HasHeaderRow = e.Value;

        var headerRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                headerSwitch,
                new Label
                {
                    Text = "Первая строка - заголовок",
                    TextColor = Ui.Text,
                    FontSize = 13,
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };

        var addRow = SmallReportButton("Добавить строку");
        addRow.Clicked += (_, _) =>
        {
            AddTableRow(block);
            RenderSelectedSection();
        };

        var removeRow = SmallReportButton("Убрать строку");
        removeRow.Clicked += (_, _) =>
        {
            RemoveTableRow(block);
            RenderSelectedSection();
        };

        var addColumn = SmallReportButton("Добавить столбец");
        addColumn.Clicked += (_, _) =>
        {
            AddTableColumn(block);
            RenderSelectedSection();
        };

        var removeColumn = SmallReportButton("Убрать столбец");
        removeColumn.Clicked += (_, _) =>
        {
            RemoveTableColumn(block);
            RenderSelectedSection();
        };

        var split = SmallReportButton("Разделить ячейки");
        split.Clicked += (_, _) =>
        {
            SplitAllTableCells(block);
            RenderSelectedSection();
        };

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow("Название таблицы"), title } },
                headerRow,
                new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    Content = BuildTableGrid(block)
                },
                WrapActions(addRow, removeRow, addColumn, removeColumn, split)
            }
        };
    }

    private View BuildTableGrid(TableReportBlockDraft block)
    {
        var columnCount = Math.Max(1, block.Rows.Max(x => x.Cells.Count));
        var grid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            MinimumWidthRequest = Math.Max(320, columnCount * 170)
        };

        for (var i = 0; i < columnCount; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(170)));

        for (var i = 0; i < block.Rows.Count; i++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var rowIndex = 0; rowIndex < block.Rows.Count; rowIndex++)
        {
            var row = block.Rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
            {
                var cell = row.Cells[columnIndex];
                if (cell.Hidden)
                    continue;

                var cellView = TableCellEditor(cell, rowIndex, columnIndex);
                Grid.SetColumn(cellView, columnIndex);
                Grid.SetRow(cellView, rowIndex);
                Grid.SetColumnSpan(cellView, Math.Min(Math.Max(1, cell.Colspan), Math.Max(1, columnCount - columnIndex)));
                Grid.SetRowSpan(cellView, Math.Min(Math.Max(1, cell.Rowspan), Math.Max(1, block.Rows.Count - rowIndex)));
                grid.Children.Add(cellView);
            }
        }

        return grid;
    }

    private View TableCellEditor(TableCellDraft cell, int rowIndex, int columnIndex)
    {
        var text = Ui.Entry($"Ячейка {rowIndex + 1}.{columnIndex + 1}");
        text.Text = cell.Text;
        text.TextChanged += (_, e) => cell.Text = e.NewTextValue ?? string.Empty;

        var colspan = Ui.Entry("Colspan", Keyboard.Numeric);
        colspan.Text = Math.Max(1, cell.Colspan).ToString();
        colspan.TextChanged += (_, e) =>
        {
            if (int.TryParse(e.NewTextValue, out var value))
                cell.Colspan = Math.Max(1, value);
        };

        var rowspan = Ui.Entry("Rowspan", Keyboard.Numeric);
        rowspan.Text = Math.Max(1, cell.Rowspan).ToString();
        rowspan.TextChanged += (_, e) =>
        {
            if (int.TryParse(e.NewTextValue, out var value))
                cell.Rowspan = Math.Max(1, value);
        };

        var spanGrid = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        AddGrid(spanGrid, new VerticalStackLayout { Spacing = 4, Children = { Ui.Eyebrow("Колонки"), colspan } }, 0, 0);
        AddGrid(spanGrid, new VerticalStackLayout { Spacing = 4, Children = { Ui.Eyebrow("Строки"), rowspan } }, 1, 0);

        return new Border
        {
            BackgroundColor = Ui.SurfaceInput,
            Stroke = Ui.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 8,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    text,
                    spanGrid
                }
            }
        };
    }

    private View ImageReportBlockEditor(ImageReportBlockDraft block, DiaryEntry entry)
    {
        var title = Ui.Entry("Название рисунка");
        title.Text = block.Title;
        title.TextChanged += (_, e) => block.Title = e.NewTextValue ?? string.Empty;

        var alt = Ui.Entry("Описание изображения");
        alt.Text = block.Alt ?? string.Empty;
        alt.TextChanged += (_, e) => block.Alt = e.NewTextValue ?? string.Empty;

        var replace = SmallReportButton(string.IsNullOrWhiteSpace(block.Base64Content) && block.AttachmentId is null
            ? "Выбрать изображение"
            : "Заменить изображение");
        replace.Clicked += async (_, _) =>
        {
            if (await PickReportImageAsync(block))
                RenderSelectedSection();
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow("Название рисунка"), title } },
                new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow("Описание"), alt } },
                ImagePreview(block, entry),
                WrapActions(replace)
            }
        };

        return stack;
    }

    private View ImagePreview(ImageReportBlockDraft block, DiaryEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(block.LocalPath) && File.Exists(block.LocalPath))
        {
            return new Image
            {
                Source = ImageSource.FromFile(block.LocalPath),
                HeightRequest = 180,
                Aspect = Aspect.AspectFit,
                BackgroundColor = Ui.SurfaceInput
            };
        }

        if (!string.IsNullOrWhiteSpace(block.Base64Content))
            return Ui.Caption($"{block.FileName} · {FormatBytes(block.SizeBytes)}");

        if (block.AttachmentId is > 0)
        {
            var attachment = entry.Attachments.FirstOrDefault(x => x.Id == block.AttachmentId.Value);
            var open = SmallReportButton("Открыть изображение");
            open.Clicked += async (_, _) => await DownloadAndOpenApiFileAsync($"api/Student/diary-attachments/{block.AttachmentId.Value}/download");

            return new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Ui.Caption(attachment is null
                        ? $"Вложение #{block.AttachmentId.Value}"
                        : $"{attachment.FileName} · {FormatBytes(attachment.SizeBytes)}"),
                    open
                }
            };
        }

        return Ui.Caption("Изображение не выбрано.");
    }

    private async Task AddImageReportBlockAsync(DayReportDraft report)
    {
        var block = CreateImageReportBlock();
        if (!await PickReportImageAsync(block))
            return;

        report.Blocks.Add(block);
        RenderSelectedSection();
    }

    private async Task<bool> PickReportImageAsync(ImageReportBlockDraft block)
    {
        FileResult? file;
        try
        {
            file = await FilePicker.Default.PickAsync(PickOptions.Images);
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or PermissionException or IOException)
        {
            ShowError(_message, $"Не удалось выбрать изображение: {ex.Message}");
            return false;
        }

        if (file is null)
            return false;

        await using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();
        if (bytes.Length > 8 * 1024 * 1024)
        {
            ShowError(_message, "Размер изображения не должен превышать 8 МБ.");
            return false;
        }

        var contentType = NormalizeImageContentType(file.ContentType, file.FileName);
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ShowError(_message, "Можно выбрать только изображение.");
            return false;
        }

        block.AttachmentId = null;
        block.UploadClientId = MakeReportId("upload");
        block.Base64Content = Convert.ToBase64String(bytes);
        block.FileName = file.FileName;
        block.ContentType = contentType;
        block.SizeBytes = bytes.Length;
        block.LocalPath = file.FullPath;
        block.ImageUrl = null;
        if (string.IsNullOrWhiteSpace(block.Title) || block.Title == "Рисунок")
            block.Title = System.IO.Path.GetFileNameWithoutExtension(file.FileName);

        ShowError(_message, string.Empty);
        return true;
    }

    private static Button SmallReportButton(string text)
    {
        var button = Ui.SecondaryButton(text);
        button.FontSize = 12;
        button.HeightRequest = 40;
        button.Padding = new Thickness(10, 0);
        return button;
    }

    private static Grid WrapActions(params View[] actions)
    {
        var layout = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };

        for (var i = 0; i < actions.Length; i++)
        {
            if (i % 2 == 0)
                layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var action = actions[i];
            Grid.SetColumn(action, i % 2);
            Grid.SetRow(action, i / 2);
            layout.Children.Add(action);
        }

        return layout;
    }

    private static string ReportBlockTitle(ReportBlockDraft block)
    {
        return block switch
        {
            TextReportBlockDraft => "Текст",
            TableReportBlockDraft => "Таблица",
            ImageReportBlockDraft => "Рисунок",
            _ => "Блок"
        };
    }

    private static void MoveReportBlock(DayReportDraft report, int index, int direction)
    {
        var target = index + direction;
        if (index < 0 || index >= report.Blocks.Count || target < 0 || target >= report.Blocks.Count)
            return;

        (report.Blocks[index], report.Blocks[target]) = (report.Blocks[target], report.Blocks[index]);
    }

    private static TextReportBlockDraft CreateTextReportBlock(string? content = null, string? mode = null)
    {
        return new TextReportBlockDraft
        {
            Id = MakeReportId("text"),
            Content = content ?? string.Empty,
            Mode = string.IsNullOrWhiteSpace(mode) ? "paragraph" : mode.Trim()
        };
    }

    private static TableReportBlockDraft CreateTableReportBlock()
    {
        var block = new TableReportBlockDraft
        {
            Id = MakeReportId("table"),
            Title = "Таблица по результатам работы",
            HasHeaderRow = true
        };

        block.Rows.Add(new TableRowDraft
        {
            Id = MakeReportId("row"),
            Cells =
            {
                new TableCellDraft { Id = MakeReportId("cell"), Text = "Показатель" },
                new TableCellDraft { Id = MakeReportId("cell"), Text = "Значение" },
                new TableCellDraft { Id = MakeReportId("cell"), Text = "Комментарий" }
            }
        });
        block.Rows.Add(CreateTableRow(3));
        return block;
    }

    private static ImageReportBlockDraft CreateImageReportBlock()
    {
        return new ImageReportBlockDraft
        {
            Id = MakeReportId("image"),
            Title = "Рисунок"
        };
    }

    private static TableRowDraft CreateTableRow(int columnCount)
    {
        var row = new TableRowDraft { Id = MakeReportId("row") };
        for (var i = 0; i < Math.Max(1, columnCount); i++)
            row.Cells.Add(new TableCellDraft { Id = MakeReportId("cell") });

        return row;
    }

    private static void EnsureTableShape(TableReportBlockDraft block)
    {
        if (block.Rows.Count == 0)
            block.Rows.Add(CreateTableRow(3));

        var columnCount = Math.Max(1, block.Rows.Max(x => x.Cells.Count));
        foreach (var row in block.Rows)
        {
            while (row.Cells.Count < columnCount)
                row.Cells.Add(new TableCellDraft { Id = MakeReportId("cell") });
        }
    }

    private static void AddTableRow(TableReportBlockDraft block)
    {
        EnsureTableShape(block);
        block.Rows.Add(CreateTableRow(block.Rows.Max(x => x.Cells.Count)));
    }

    private static void RemoveTableRow(TableReportBlockDraft block)
    {
        if (block.Rows.Count <= 1)
            return;

        block.Rows.RemoveAt(block.Rows.Count - 1);
    }

    private static void AddTableColumn(TableReportBlockDraft block)
    {
        EnsureTableShape(block);
        foreach (var row in block.Rows)
            row.Cells.Add(new TableCellDraft { Id = MakeReportId("cell") });
    }

    private static void RemoveTableColumn(TableReportBlockDraft block)
    {
        EnsureTableShape(block);
        var columnCount = block.Rows.Max(x => x.Cells.Count);
        if (columnCount <= 1)
            return;

        foreach (var row in block.Rows.Where(x => x.Cells.Count > 0))
            row.Cells.RemoveAt(row.Cells.Count - 1);
    }

    private static void SplitAllTableCells(TableReportBlockDraft block)
    {
        foreach (var cell in block.Rows.SelectMany(x => x.Cells))
        {
            cell.Colspan = 1;
            cell.Rowspan = 1;
            cell.Hidden = false;
        }
    }

    private static string SerializeReportDocument(DayReportDraft report)
    {
        var blocks = new JsonArray();
        foreach (var block in report.Blocks)
        {
            switch (block)
            {
                case TextReportBlockDraft text:
                    blocks.Add(new JsonObject
                    {
                        ["type"] = "text",
                        ["id"] = text.Id,
                        ["content"] = text.Content ?? string.Empty,
                        ["mode"] = string.IsNullOrWhiteSpace(text.Mode) ? "paragraph" : text.Mode
                    });
                    break;

                case TableReportBlockDraft table:
                    var rows = new JsonArray();
                    foreach (var row in table.Rows)
                    {
                        var cells = new JsonArray();
                        foreach (var cell in row.Cells)
                        {
                            cells.Add(new JsonObject
                            {
                                ["id"] = cell.Id,
                                ["text"] = cell.Text ?? string.Empty,
                                ["colspan"] = Math.Max(1, cell.Colspan),
                                ["rowspan"] = Math.Max(1, cell.Rowspan),
                                ["hidden"] = cell.Hidden
                            });
                        }

                        rows.Add(new JsonObject
                        {
                            ["id"] = row.Id,
                            ["cells"] = cells
                        });
                    }

                    blocks.Add(new JsonObject
                    {
                        ["type"] = "table",
                        ["id"] = table.Id,
                        ["title"] = table.Title ?? string.Empty,
                        ["hasHeaderRow"] = table.HasHeaderRow,
                        ["rows"] = rows
                    });
                    break;

                case ImageReportBlockDraft image:
                    var imageNode = new JsonObject
                    {
                        ["type"] = "image",
                        ["id"] = image.Id,
                        ["title"] = image.Title ?? string.Empty,
                        ["alt"] = image.Alt ?? string.Empty,
                        ["fileName"] = image.FileName ?? string.Empty,
                        ["mimeType"] = image.ContentType ?? string.Empty,
                        ["size"] = image.SizeBytes
                    };

                    if (image.AttachmentId is > 0)
                        imageNode["attachmentId"] = image.AttachmentId.Value;
                    else if (!string.IsNullOrWhiteSpace(image.UploadClientId))
                        imageNode["uploadClientId"] = image.UploadClientId;

                    if (!string.IsNullOrWhiteSpace(image.ImageUrl))
                        imageNode["imageUrl"] = image.ImageUrl;

                    blocks.Add(imageNode);
                    break;
            }
        }

        return new JsonObject
        {
            ["version"] = 3,
            ["type"] = "practice-day-report",
            ["blocks"] = blocks,
            ["attachments"] = BuildReportAttachmentIndex(report)
        }.ToJsonString();
    }

    private static JsonArray BuildReportAttachmentIndex(DayReportDraft report)
    {
        var attachments = new JsonArray();
        foreach (var image in report.Blocks.OfType<ImageReportBlockDraft>())
        {
            if (image.AttachmentId is not > 0)
                continue;

            attachments.Add(new JsonObject
            {
                ["id"] = image.AttachmentId.Value,
                ["type"] = "image",
                ["filename"] = image.FileName ?? string.Empty,
                ["mimeType"] = image.ContentType ?? string.Empty,
                ["size"] = image.SizeBytes
            });
        }

        return attachments;
    }

    private static List<int> GetReferencedAttachmentIds(DayReportDraft report)
    {
        return report.Blocks
            .OfType<ImageReportBlockDraft>()
            .Where(x => x.AttachmentId is > 0)
            .Select(x => x.AttachmentId!.Value)
            .Distinct()
            .ToList();
    }

    private static List<DiaryFigureUpload> BuildPendingFigureUploads(DayReportDraft report)
    {
        return report.Blocks
            .OfType<ImageReportBlockDraft>()
            .Where(x => !string.IsNullOrWhiteSpace(x.Base64Content) && !string.IsNullOrWhiteSpace(x.UploadClientId))
            .Select((x, index) => new DiaryFigureUpload
            {
                ClientId = x.UploadClientId,
                Caption = x.Title,
                FileName = x.FileName,
                ContentType = x.ContentType,
                Base64Content = x.Base64Content,
                SortOrder = index + 1
            })
            .ToList();
    }

    private static DayReportDraft ParseReportDocumentDraft(string? value, IEnumerable<DiaryAttachment> attachments)
    {
        var report = new DayReportDraft();
        var attachmentList = attachments.ToList();
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                if (JsonNode.Parse(value) is JsonObject document)
                {
                    var blocks = document["blocks"] as JsonArray ?? document["content"] as JsonArray;
                    if (blocks is not null)
                    {
                        foreach (var node in blocks.OfType<JsonObject>())
                        {
                            var parsed = ParseReportBlockDraft(node);
                            if (parsed is not null)
                                report.Blocks.Add(parsed);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                report.Blocks.Add(CreateTextReportBlock(value));
            }
        }

        var attachmentById = attachmentList.ToDictionary(x => x.Id);
        foreach (var image in report.Blocks.OfType<ImageReportBlockDraft>())
        {
            if (image.AttachmentId is not > 0 || !attachmentById.TryGetValue(image.AttachmentId.Value, out var attachment))
                continue;

            if (string.IsNullOrWhiteSpace(image.Title) || image.Title == "Рисунок")
                image.Title = string.IsNullOrWhiteSpace(attachment.Caption) ? attachment.FileName : attachment.Caption;
            if (string.IsNullOrWhiteSpace(image.FileName))
                image.FileName = attachment.FileName;
            if (string.IsNullOrWhiteSpace(image.ContentType))
                image.ContentType = attachment.ContentType;
            if (image.SizeBytes <= 0)
                image.SizeBytes = attachment.SizeBytes;
        }

        var usedAttachmentIds = report.Blocks
            .OfType<ImageReportBlockDraft>()
            .Where(x => x.AttachmentId is > 0)
            .Select(x => x.AttachmentId!.Value)
            .ToHashSet();

        foreach (var attachment in attachmentList.OrderBy(x => x.SortOrder))
        {
            if (usedAttachmentIds.Contains(attachment.Id))
                continue;

            report.Blocks.Add(new ImageReportBlockDraft
            {
                Id = MakeReportId("image"),
                Title = string.IsNullOrWhiteSpace(attachment.Caption) ? attachment.FileName : attachment.Caption,
                AttachmentId = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes
            });
        }

        if (report.Blocks.Count == 0)
            report.Blocks.Add(CreateTextReportBlock());

        return report;
    }

    private static ReportBlockDraft? ParseReportBlockDraft(JsonObject node)
    {
        var type = GetNodeString(node, "type") ?? string.Empty;
        if (type is "text" or "paragraph" or "heading")
        {
            var content = GetNodeString(node, "content", "text");
            if (string.IsNullOrWhiteSpace(content))
                content = StripHtml(GetNodeString(node, "html"));

            var block = CreateTextReportBlock(
                content ?? string.Empty,
                GetNodeString(node, "mode") ?? (type == "heading" ? "heading" : "paragraph"));
            block.Id = FirstNotEmpty(GetNodeString(node, "id"), block.Id);
            return block;
        }

        if (type == "table")
        {
            var table = new TableReportBlockDraft
            {
                Id = FirstNotEmpty(GetNodeString(node, "id"), MakeReportId("table")),
                Title = GetNodeString(node, "title", "caption") ?? "Таблица",
                HasHeaderRow = GetNodeBool(node, true, "hasHeaderRow", "hasHeader")
            };

            if (node["rows"] is JsonArray rows)
            {
                foreach (var rowNode in rows.OfType<JsonObject>())
                {
                    var row = new TableRowDraft
                    {
                        Id = FirstNotEmpty(GetNodeString(rowNode, "id"), MakeReportId("row"))
                    };

                    if (rowNode["cells"] is JsonArray cells)
                    {
                        foreach (var cellNode in cells.OfType<JsonObject>())
                        {
                            row.Cells.Add(new TableCellDraft
                            {
                                Id = FirstNotEmpty(GetNodeString(cellNode, "id"), MakeReportId("cell")),
                                Text = GetNodeString(cellNode, "text", "content") ?? string.Empty,
                                Colspan = Math.Max(1, GetNodeInt(cellNode, 1, "colspan")),
                                Rowspan = Math.Max(1, GetNodeInt(cellNode, 1, "rowspan")),
                                Hidden = GetNodeBool(cellNode, false, "hidden")
                            });
                        }
                    }

                    if (row.Cells.Count > 0)
                        table.Rows.Add(row);
                }
            }

            if (table.Rows.Count == 0)
                return CreateTableReportBlock();

            return table;
        }

        if (type is "image" or "figure")
        {
            return new ImageReportBlockDraft
            {
                Id = FirstNotEmpty(GetNodeString(node, "id"), MakeReportId("image")),
                Title = GetNodeString(node, "title", "caption") ?? "Рисунок",
                AttachmentId = GetNodeNullableInt(node, "attachmentId"),
                UploadClientId = GetNodeString(node, "uploadClientId"),
                ImageUrl = GetNodeString(node, "imageUrl", "previewUrl"),
                Alt = GetNodeString(node, "alt"),
                FileName = GetNodeString(node, "fileName", "filename"),
                ContentType = GetNodeString(node, "mimeType", "contentType"),
                SizeBytes = GetNodeLong(node, 0, "size", "sizeBytes")
            };
        }

        return null;
    }

    private static string NormalizeImageContentType(string? contentType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var normalized = contentType.Trim().ToLowerInvariant();
            return normalized == "image/jpg" ? "image/jpeg" : normalized;
        }

        return System.IO.Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string MakeReportId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static string? GetNodeString(JsonObject node, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (node[property] is not JsonValue value)
                continue;

            try
            {
                return value.GetValue<string>();
            }
            catch
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static int GetNodeInt(JsonObject node, int fallback, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (node[property] is not JsonValue value)
                continue;

            try
            {
                return value.GetValue<int>();
            }
            catch
            {
                return int.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
            }
        }

        return fallback;
    }

    private static int? GetNodeNullableInt(JsonObject node, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (node[property] is not JsonValue value)
                continue;

            try
            {
                return value.GetValue<int>();
            }
            catch
            {
                return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
            }
        }

        return null;
    }

    private static long GetNodeLong(JsonObject node, long fallback, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (node[property] is not JsonValue value)
                continue;

            try
            {
                return value.GetValue<long>();
            }
            catch
            {
                return long.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
            }
        }

        return fallback;
    }

    private static bool GetNodeBool(JsonObject node, bool fallback, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (node[property] is not JsonValue value)
                continue;

            try
            {
                return value.GetValue<bool>();
            }
            catch
            {
                return bool.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
            }
        }

        return fallback;
    }

    private View IntroductionSection()
    {
        return _introductionEditing ? IntroductionEditSection() : IntroductionReadonlySection();
    }

    private View IntroductionReadonlySection()
    {
        var p = _practice!;
        var edit = Ui.PrimaryButton("Редактировать");
        edit.Clicked += (_, _) =>
        {
            _introductionEditing = true;
            RenderSelectedSection();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                HeaderWithAction("Введение и содержание отчёта", edit),
                CommentsFor("introduction"),
                ReadOnlyGrid(new[]
                {
                    Row("Основная цель", p.IntroductionMainGoal),
                    Row("Выполняемые обязанности", p.StudentDuties),
                    Row("Предоставленные материалы", p.ProvidedMaterialsDescription),
                    Row("График работы", p.WorkScheduleDescription),
                    Row("Виды работ", FormatReportItems(p.ReportItems, "IntroductionWorkType")),
                    Row("Программные средства и технологии", FormatReportItems(p.ReportItems, "IntroductionSoftwareTechnology"))
                })
            }
        });
    }

    private View IntroductionEditSection()
    {
        var p = _practice!;
        var goal = LabeledEditor("Основная цель практики", p.IntroductionMainGoal, 4);
        var duties = LabeledEditor("Выполняемые обязанности", p.StudentDuties, 4);
        var materials = LabeledEditor("Предоставленные материалы", p.ProvidedMaterialsDescription, 3);
        var schedule = LabeledEditor("График работы", p.WorkScheduleDescription, 3);
        var workTypes = LabeledEditor("Виды работ, по одному на строку", LinesForCategory(p.ReportItems, "IntroductionWorkType"), 5);
        var technologies = LabeledEditor("Программные средства и технологии, по одному на строку", LinesForCategory(p.ReportItems, "IntroductionSoftwareTechnology"), 5);

        var save = Ui.PrimaryButton("Сохранить введение");
        var cancel = Ui.SecondaryButton("Отменить");
        cancel.Clicked += (_, _) =>
        {
            _introductionEditing = false;
            RenderSelectedSection();
        };
        save.Clicked += async (_, _) =>
        {
            p.IntroductionMainGoal = goal.Input.Text;
            p.StudentDuties = duties.Input.Text;
            p.ProvidedMaterialsDescription = materials.Input.Text;
            p.WorkScheduleDescription = schedule.Input.Text;

            var savedOrganization = await _api.SaveOrganizationAsync(_assignmentId, p);
            if (!savedOrganization.Success || savedOrganization.Data is null)
            {
                ShowError(_message, ErrorText(savedOrganization, "Не удалось сохранить введение."));
                return;
            }

            var items = PreserveReportItems(savedOrganization.Data.ReportItems, "IntroductionWorkType", "IntroductionSoftwareTechnology");
            items.AddRange(ParseLineItems(workTypes.Input.Text, "IntroductionWorkType"));
            items.AddRange(ParseLineItems(technologies.Input.Text, "IntroductionSoftwareTechnology"));

            _introductionEditing = false;
            var savedItems = await _api.SaveReportItemsAsync(_assignmentId, items);
            if (!savedItems.Success || savedItems.Data is null)
            {
                ShowError(_message, ErrorText(savedItems, "Не удалось сохранить таблицы введения."));
                _practice = savedOrganization.Data;
                RenderAll();
                return;
            }

            _practice = savedItems.Data;
            ShowError(_message, string.Empty);
            RenderAll();
        };

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Title("Редактирование введения", 18),
                goal.View, duties.View, materials.View, schedule.View, workTypes.View, technologies.View,
                Ui.ActionRow(cancel, save)
            }
        });
    }

    private View TechnicalSection()
    {
        return _technicalEditing ? TechnicalEditSection() : TechnicalReadonlySection();
    }

    private View TechnicalReadonlySection()
    {
        var values = GetTechnicalValues(_practice!.ReportItems);
        var edit = Ui.PrimaryButton("Редактировать");
        edit.Clicked += (_, _) =>
        {
            _technicalEditing = true;
            RenderSelectedSection();
        };

        var rows = new List<(string Label, string? Value)> { Row("Компьютер", values.ComputerName) };
        rows.AddRange(TechnicalCharacteristics.Select(label => Row(label, values.Characteristics.TryGetValue(label, out var value) ? value : null)));

        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                HeaderWithAction("Технические средства", edit),
                ReadOnlyGrid(rows)
            }
        });
    }

    private View TechnicalEditSection()
    {
        var values = GetTechnicalValues(_practice!.ReportItems);
        var computer = LabeledEntry("Компьютер", values.ComputerName);
        var fields = TechnicalCharacteristics
            .Select(label => (Label: label, Field: LabeledEntry(label, values.Characteristics.TryGetValue(label, out var value) ? value : null)))
            .ToList();

        var save = Ui.PrimaryButton("Сохранить технические средства");
        var cancel = Ui.SecondaryButton("Отменить");
        cancel.Clicked += (_, _) =>
        {
            _technicalEditing = false;
            RenderSelectedSection();
        };

        save.Clicked += async (_, _) =>
        {
            var items = PreserveReportItems(_practice!.ReportItems, "TechnicalTool");
            if (!string.IsNullOrWhiteSpace(computer.Input.Text))
            {
                items.Add(new ReportItem { Category = "TechnicalTool", Name = "Компьютер", Description = computer.Input.Text });
            }

            items.AddRange(fields
                .Where(x => !string.IsNullOrWhiteSpace(x.Field.Input.Text))
                .Select(x => new ReportItem { Category = "TechnicalTool", Name = x.Label, Description = x.Field.Input.Text }));

            _technicalEditing = false;
            await SavePracticeResultAsync(() => _api.SaveReportItemsAsync(_assignmentId, items));
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 12,
            Children = { Ui.Title("Редактирование технических средств", 18), computer.View }
        };
        foreach (var field in fields)
            stack.Children.Add(field.Field.View);
        stack.Children.Add(Ui.ActionRow(cancel, save));
        return Ui.Card(stack);
    }

    private View SourcesSection()
    {
        return _sourcesEditing ? SourcesEditSection() : SourcesReadonlySection();
    }

    private View SourcesReadonlySection()
    {
        var edit = Ui.PrimaryButton(_practice!.Sources.Count == 0 ? "Добавить источники" : "Редактировать");
        edit.Clicked += (_, _) =>
        {
            _sourceDrafts.Clear();
            _sourceDrafts.AddRange(_practice!.Sources.Select(x => new SourceDraft(x.Title, x.Url, x.Description)));
            if (_sourceDrafts.Count == 0)
                _sourceDrafts.Add(new SourceDraft());
            _sourcesEditing = true;
            RenderSelectedSection();
        };

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(HeaderWithAction("Использованные источники", edit));
        stack.Children.Add(CommentsFor("sources"));

        if (_practice!.Sources.Count == 0)
        {
            stack.Children.Add(Ui.EmptyState("Источники не указаны", "Добавьте литературу, сайты, документацию и другие материалы, которые использовались в отчёте."));
        }
        else
        {
            foreach (var source in _practice.Sources.OrderBy(x => x.SortOrder))
                stack.Children.Add(SourceReadonlyCard(source));
        }

        return Ui.Card(stack);
    }

    private View SourcesEditSection()
    {
        _sourceEditors.Clear();
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(Ui.Title("Редактирование источников", 18));

        for (var i = 0; i < _sourceDrafts.Count; i++)
        {
            var draft = _sourceDrafts[i];
            var title = LabeledEntry("Название источника", draft.Title);
            var url = LabeledEntry("Ссылка", draft.Url, Keyboard.Url);
            var description = LabeledEntry("Комментарий", draft.Description);
            var remove = Ui.SecondaryButton("Убрать");
            var index = i;
            remove.Clicked += (_, _) =>
            {
                CaptureSourceDrafts();
                if (index >= 0 && index < _sourceDrafts.Count)
                    _sourceDrafts.RemoveAt(index);
                if (_sourceDrafts.Count == 0)
                    _sourceDrafts.Add(new SourceDraft());
                RenderSelectedSection();
            };

            _sourceEditors.Add(new SourceEditorRow(title.Input, url.Input, description.Input));
            stack.Children.Add(Ui.Panel(new VerticalStackLayout
            {
                Spacing = 10,
                Children = { Ui.Eyebrow($"Источник {i + 1}"), title.View, url.View, description.View, remove }
            }));
        }

        var add = Ui.SecondaryButton("Добавить источник");
        add.Clicked += (_, _) =>
        {
            CaptureSourceDrafts();
            _sourceDrafts.Add(new SourceDraft());
            RenderSelectedSection();
        };

        var save = Ui.PrimaryButton("Сохранить источники");
        var cancel = Ui.SecondaryButton("Отменить");
        cancel.Clicked += (_, _) =>
        {
            _sourcesEditing = false;
            RenderSelectedSection();
        };
        save.Clicked += async (_, _) =>
        {
            CaptureSourceDrafts();
            var items = _sourceDrafts
                .Where(x => !string.IsNullOrWhiteSpace(x.Title) || !string.IsNullOrWhiteSpace(x.Url) || !string.IsNullOrWhiteSpace(x.Description))
                .Select(x => new PracticeSource { Title = x.Title ?? string.Empty, Url = x.Url, Description = x.Description })
                .ToList();
            _sourcesEditing = false;
            await SavePracticeResultAsync(() => _api.SaveSourcesAsync(_assignmentId, items));
        };

        stack.Children.Add(add);
        stack.Children.Add(Ui.ActionRow(cancel, save));
        return Ui.Card(stack);
    }

    private View AppendicesSection()
    {
        var p = _practice!;
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(Ui.Title("Приложения", 18));
        stack.Children.Add(Notice("Файлы хранятся в практике и попадают в материалы отчёта. Поддерживаются DOCX, PDF, архивы, код и изображения до 15 МБ.", Ui.PrimarySoft));

        var title = LabeledEntry("Название приложения", null);
        var description = LabeledEditor("Описание", null, 3);
        var upload = Ui.PrimaryButton("Выбрать и загрузить файл");
        upload.Clicked += async (_, _) =>
        {
            var file = await FilePicker.Default.PickAsync();
            if (file is null)
                return;

            var result = await _api.UploadAppendixAsync(_assignmentId, file, title.Input.Text, description.Input.Text);
            if (!result.Success || result.Data is null)
            {
                ShowError(_message, ErrorText(result, "Не удалось загрузить приложение."));
                return;
            }

            _practice = result.Data;
            ShowError(_message, string.Empty);
            RenderAll();
        };

        stack.Children.Add(Ui.Panel(new VerticalStackLayout
        {
            Spacing = 10,
            Children = { Ui.Eyebrow("Новое приложение"), title.View, description.View, upload }
        }));

        if (p.Appendices.Count == 0)
        {
            stack.Children.Add(Ui.EmptyState("Файлы не загружены", "После загрузки приложения появятся в списке с действиями открытия и удаления."));
        }
        else
        {
            foreach (var appendix in p.Appendices.OrderByDescending(x => x.CreatedAtUtc))
                stack.Children.Add(AppendixCard(appendix));
        }

        return Ui.Card(stack);
    }

    private View DocumentsSection()
    {
        var p = _practice!;
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(Ui.Title("Формирование документов", 18));

        stack.Children.Add(DocumentCard(
            "Аттестационный лист",
            "Характеристика выполнения работ и компетенций.",
            $"Student/DownloadAttestation?assignmentId={p.AssignmentId}",
            $"Student/DownloadAttestationPdf?assignmentId={p.AssignmentId}"));

        stack.Children.Add(DocumentCard(
            "Дневник практики",
            "Краткие записи по каждому рабочему дню.",
            $"Student/DownloadPracticeDiary?assignmentId={p.AssignmentId}",
            $"Student/DownloadPracticeDiaryPdf?assignmentId={p.AssignmentId}"));

        stack.Children.Add(DocumentCard(
            "Отчёт по практике",
            "Итоговый отчёт на основе введения, дневника, источников и приложений.",
            $"Student/DownloadPracticeReport?assignmentId={p.AssignmentId}",
            $"Student/DownloadPracticeReportPdf?assignmentId={p.AssignmentId}"));

        return Ui.Card(stack);
    }

    private View DocumentCard(string title, string description, string docxUrl, string pdfUrl)
    {
        var docx = Ui.PrimaryButton("DOCX");
        docx.Clicked += async (_, _) => await DownloadAndShareWebFileAsync(docxUrl);
        var pdf = Ui.SecondaryButton("PDF");
        pdf.Clicked += async (_, _) => await DownloadAndOpenWebFileAsync(pdfUrl);

        return Ui.Panel(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Title(title, 16),
                Ui.Caption(description),
                Ui.ActionRow(docx, pdf)
            }
        });
    }

    private View CompetencySection(string title, IEnumerable<(string Code, string Description, string Details)> items)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(Ui.Title(title, 18));
        var list = items.ToList();
        if (list.Count == 0)
        {
            stack.Children.Add(Ui.EmptyState("Нет данных", "Компетенции для этой практики не указаны."));
        }
        else
        {
            foreach (var item in list)
            {
                stack.Children.Add(Ui.Panel(new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        Ui.Eyebrow(item.Code),
                        Ui.Title(item.Description, 15),
                        string.IsNullOrWhiteSpace(item.Details) ? new BoxView { HeightRequest = 0 } : Ui.Caption(item.Details)
                    }
                }));
            }
        }

        return Ui.Card(stack);
    }

    private View SectionCard(string title, View content)
    {
        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children = { Ui.Title(title, 18), content }
        });
    }

    private View ReadOnlyGrid(IEnumerable<(string Label, string? Value)> rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        foreach (var row in rows)
            stack.Children.Add(ReadOnlyItem(row.Label, row.Value));
        return stack;
    }

    private View ReadOnlyItem(string label, string? value)
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

    private View HeaderWithAction(string title, Button action)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        AddGrid(grid, Ui.Title(title, 18), 0, 0);
        action.WidthRequest = 150;
        AddGrid(grid, action, 1, 0);
        return grid;
    }

    private View Notice(string text, Color color)
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

    private (View View, Entry Input) LabeledEntry(string label, string? value, Keyboard? keyboard = null)
    {
        var entry = Ui.Entry(label, keyboard);
        entry.Text = value ?? string.Empty;
        return (new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow(label), entry } }, entry);
    }

    private (View View, Editor Input) LabeledEditor(string label, string? value, int lines)
    {
        var editor = Ui.Editor(label, lines);
        editor.Text = value ?? string.Empty;
        return (new VerticalStackLayout { Spacing = 6, Children = { Ui.Eyebrow(label), editor } }, editor);
    }

    private View DiaryReview(DiaryEntry entry)
    {
        if (entry.Id == 0)
            return Notice("День ещё не сохранён и не отправлен на проверку.", Ui.Muted);

        if (entry.IsReviewed)
        {
            var grade = entry.SupervisorGrade?.ToString() ?? "без оценки";
            var comment = string.IsNullOrWhiteSpace(entry.SupervisorComment) ? "Комментарий не оставлен." : entry.SupervisorComment;
            return Notice($"Проверено руководителем: {grade}. {comment}", Ui.Accent);
        }

        return Notice("День ожидает проверки руководителем. Если изменить запись, проверка будет сброшена.", Ui.PrimarySoft);
    }

    private View AttachmentsView(IEnumerable<DiaryAttachment> attachments)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        var list = attachments.ToList();
        stack.Children.Add(Ui.Eyebrow("Изображения дня"));
        if (list.Count == 0)
        {
            stack.Children.Add(Ui.Caption("Изображения пока не прикреплены."));
            return stack;
        }

        foreach (var item in list)
        {
            var open = Ui.SecondaryButton("Открыть");
            open.Clicked += async (_, _) => await DownloadAndOpenApiFileAsync($"api/Student/diary-attachments/{item.Id}/download");
            stack.Children.Add(Ui.Panel(new VerticalStackLayout
            {
                Spacing = 6,
                Children = { Ui.Title(item.Caption, 15), Ui.Caption($"{item.FileName} · {FormatBytes(item.SizeBytes)}"), open }
            }));
        }

        return stack;
    }

    private View SourceReadonlyCard(PracticeSource source)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Ui.Title(source.Title, 15),
                string.IsNullOrWhiteSpace(source.Url) ? new BoxView { HeightRequest = 0 } : Ui.Caption(source.Url),
                string.IsNullOrWhiteSpace(source.Description) ? new BoxView { HeightRequest = 0 } : Ui.Caption(source.Description)
            }
        };

        if (!string.IsNullOrWhiteSpace(source.Url) && Uri.TryCreate(source.Url, UriKind.Absolute, out var uri))
        {
            var open = Ui.SecondaryButton("Открыть ссылку");
            open.Clicked += async (_, _) => await Browser.Default.OpenAsync(uri);
            stack.Children.Add(open);
        }

        return Ui.Panel(stack);
    }

    private View AppendixCard(PracticeAppendix appendix)
    {
        var open = Ui.SecondaryButton("Открыть");
        open.FontSize = 13;
        open.Clicked += async (_, _) => await DownloadAndOpenApiFileAsync($"api/Student/appendices/{appendix.Id}/download");
        var share = Ui.SecondaryButton("Поделиться");
        share.FontSize = 13;
        share.Clicked += async (_, _) => await DownloadAndShareApiFileAsync($"api/Student/appendices/{appendix.Id}/download");
        var delete = Ui.SecondaryButton("Удалить");
        delete.FontSize = 13;
        delete.TextColor = Ui.Danger;
        delete.Clicked += async (_, _) =>
        {
            var confirm = await DisplayAlert("Удалить приложение", $"Удалить «{appendix.Title}»?", "Удалить", "Отмена");
            if (!confirm)
                return;

            var result = await _api.DeleteAppendixAsync(appendix.Id);
            if (!result.Success)
            {
                ShowError(_message, ErrorText(result, "Не удалось удалить приложение."));
                return;
            }

            await LoadAsync();
        };

        return Ui.Panel(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.Title(appendix.Title, 15),
                Ui.Caption(string.IsNullOrWhiteSpace(appendix.Description) ? appendix.FileName : appendix.Description!),
                Ui.Caption($"{appendix.FileName} · {FormatBytes(appendix.SizeBytes)} · {Ui.Date(appendix.CreatedAtUtc)}"),
                AppendixActionRow(open, share, delete)
            }
        });
    }

    private static Grid AppendixActionRow(Button open, Button share, Button delete)
    {
        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(0.85, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1.35, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(0.85, GridUnitType.Star))
            }
        };

        AddGrid(grid, open, 0, 0);
        AddGrid(grid, share, 1, 0);
        AddGrid(grid, delete, 2, 0);
        return grid;
    }

    private View CommentsFor(string sectionKey)
    {
        var comment = _practice?.SectionComments.FirstOrDefault(x => x.SectionKey == sectionKey);
        if (comment is null)
            return new BoxView { HeightRequest = 0 };

        return Notice($"Комментарий руководителя: {comment.Comment}", Ui.Warning);
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
        RenderAll();
    }

    private async Task DownloadAndOpenApiFileAsync(string url)
    {
        await OpenFileResultAsync(await _api.DownloadApiFileAsync(url), shareOnly: false);
    }

    private async Task DownloadAndShareApiFileAsync(string url)
    {
        await OpenFileResultAsync(await _api.DownloadApiFileAsync(url), shareOnly: true);
    }

    private async Task DownloadAndOpenWebFileAsync(string url)
    {
        await OpenFileResultAsync(await _api.DownloadWebDocumentAsync(url), shareOnly: false);
    }

    private async Task DownloadAndShareWebFileAsync(string url)
    {
        await OpenFileResultAsync(await _api.DownloadWebDocumentAsync(url), shareOnly: true);
    }

    private async Task OpenFileResultAsync(ApiResult<FileDownload> result, bool shareOnly)
    {
        if (!result.Success || result.Data is null)
        {
            var message = ErrorText(result, "Не удалось получить файл.");
            if (message.Contains("soffice", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("LibreOffice", StringComparison.OrdinalIgnoreCase))
            {
                message = "PDF не сформирован: на веб-сервере не найден LibreOffice/soffice. DOCX при этом можно скачать.";
            }

            ShowError(_message, message);
            return;
        }

        if (shareOnly)
            await _files.ShareAsync(result.Data);
        else
            await _files.OpenAsync(result.Data);
    }

    private void CaptureSourceDrafts()
    {
        _sourceDrafts.Clear();
        _sourceDrafts.AddRange(_sourceEditors.Select(x => new SourceDraft(x.Title.Text, x.Url.Text, x.Description.Text)));
    }

    private static List<ReportItem> PreserveReportItems(IEnumerable<ReportItem> source, params string[] removedCategories)
    {
        var removed = new HashSet<string>(removedCategories, StringComparer.Ordinal);
        return source
            .Where(x => !removed.Contains(x.Category))
            .Select(x => new ReportItem { Category = x.Category, Name = x.Name, Description = x.Description })
            .ToList();
    }

    private static IEnumerable<ReportItem> ParseLineItems(string? text, string category)
    {
        return (text ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new ReportItem { Category = category, Name = x.Trim() });
    }

    private static string LinesForCategory(IEnumerable<ReportItem> items, string category)
    {
        return string.Join(Environment.NewLine, items
            .Where(x => x.Category == category)
            .OrderBy(x => x.SortOrder)
            .Select(x => string.IsNullOrWhiteSpace(x.Description) ? x.Name : $"{x.Name} - {x.Description}"));
    }

    private static string FormatReportItems(IEnumerable<ReportItem> items, string category)
    {
        var lines = items
            .Where(x => x.Category == category)
            .OrderBy(x => x.SortOrder)
            .Select((x, i) => $"{i + 1}. {x.Name}{(string.IsNullOrWhiteSpace(x.Description) ? string.Empty : $" - {x.Description}")}")
            .ToList();

        return lines.Count == 0 ? null! : string.Join(Environment.NewLine, lines);
    }

    private static TechnicalValues GetTechnicalValues(IEnumerable<ReportItem> items)
    {
        var values = new TechnicalValues();
        foreach (var item in items.Where(x => x.Category == "TechnicalTool").OrderBy(x => x.SortOrder))
        {
            if (item.Name.Equals("Компьютер", StringComparison.OrdinalIgnoreCase))
                values.ComputerName = item.Description ?? string.Empty;
            else if (TechnicalCharacteristics.Contains(item.Name))
                values.Characteristics[item.Name] = item.Description ?? string.Empty;
            else if (string.IsNullOrWhiteSpace(values.ComputerName))
                values.ComputerName = item.Name;
        }

        return values;
    }

    private static string ExtractReportPlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            var blocks = root.TryGetProperty("blocks", out var blocksElement)
                ? blocksElement
                : root.TryGetProperty("content", out var contentElement)
                    ? contentElement
                    : default;

            if (blocks.ValueKind != JsonValueKind.Array)
                return value;

            var lines = new List<string>();
            foreach (var block in blocks.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : string.Empty;
                if (type is "text" or "paragraph" or "heading")
                {
                    var text = GetString(block, "content") ?? GetString(block, "text") ?? StripHtml(GetString(block, "html"));
                    if (!string.IsNullOrWhiteSpace(text))
                        lines.Add(text.Trim());
                }
                else if (type == "table")
                {
                    var title = GetString(block, "title");
                    if (!string.IsNullOrWhiteSpace(title))
                        lines.Add(title);
                    if (block.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var row in rows.EnumerateArray())
                        {
                            if (!row.TryGetProperty("cells", out var cells) || cells.ValueKind != JsonValueKind.Array)
                                continue;
                            var cellText = cells.EnumerateArray()
                                .Select(cell => GetString(cell, "text"))
                                .Where(text => !string.IsNullOrWhiteSpace(text));
                            lines.Add(string.Join(" | ", cellText));
                        }
                    }
                }
                else if (type == "image" || type == "figure")
                {
                    var caption = GetString(block, "title") ?? GetString(block, "caption") ?? GetString(block, "alt");
                    lines.Add(string.IsNullOrWhiteSpace(caption) ? "[изображение]" : $"[изображение] {caption}");
                }
            }

            return string.Join(Environment.NewLine + Environment.NewLine, lines.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch
        {
            return value;
        }
    }

    private static string BuildTextReportJson(string? text)
    {
        var content = (text ?? string.Empty).Trim();
        var document = new
        {
            version = 3,
            type = "practice-day-report",
            blocks = new[]
            {
                new
                {
                    type = "text",
                    id = $"text-{Guid.NewGuid():N}",
                    content,
                    mode = "paragraph"
                }
            }
        };

        return JsonSerializer.Serialize(document);
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var builder = new StringBuilder(value.Length);
        var inTag = false;
        foreach (var ch in value)
        {
            if (ch == '<')
            {
                inTag = true;
                continue;
            }

            if (ch == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                builder.Append(ch);
        }

        return builder.ToString();
    }

    private static IEnumerable<DateTime> GetWorkDates(PracticeDetails practice)
    {
        for (var date = practice.StartDate.Date; date <= practice.EndDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                yield return date;
        }
    }

    private static DateTime GetDefaultDiaryDate(PracticeDetails practice)
    {
        var today = DateTime.Today;
        var dates = GetWorkDates(practice).ToList();
        return dates.FirstOrDefault(x => x.Date == today.Date) != default
            ? today.Date
            : dates.FirstOrDefault(x => !practice.DiaryEntries.Any(e => e.WorkDate.Date == x.Date), dates.LastOrDefault());
    }

    private static string FormatDateRange(PracticeListItem practice)
    {
        return $"{Ui.Date(practice.StartDate)} - {Ui.Date(practice.EndDate)}";
    }

    private static string FormatPracticeIndex(string? value)
    {
        var normalized = StripAcademicPrefix(value, "ПП");
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"ПП.{normalized}";
    }

    private static string FormatProfessionalModuleCode(string? value)
    {
        var normalized = StripAcademicPrefix(value, "ПМ");
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"ПМ.{normalized}";
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

    private static string FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }

    private static string ShortWeekday(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Пн",
            DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday => "Чт",
            DayOfWeek.Friday => "Пт",
            _ => string.Empty
        };
    }

    private static string FormatBytes(long bytes)
    {
        return bytes < 1024 * 1024
            ? $"{Math.Max(1, bytes / 1024)} КБ"
            : $"{bytes / 1024d / 1024d:0.0} МБ";
    }

    private static (string Label, string? Value) Row(string label, string? value) => (label, value);

    private static void AddGrid(Grid grid, View view, int column, int row, int columnSpan = 1)
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        if (columnSpan > 1)
            Grid.SetColumnSpan(view, columnSpan);
        grid.Children.Add(view);
    }

    private sealed record PracticeTab(string Key, string Title);
    private sealed record SourceDraft(string? Title = null, string? Url = null, string? Description = null);
    private sealed record SourceEditorRow(Entry Title, Entry Url, Entry Description);

    private sealed class DiaryDayDraft
    {
        public string ShortDescription { get; set; } = string.Empty;
        public DayReportDraft Report { get; set; } = new();
    }

    private sealed class DayReportDraft
    {
        public List<ReportBlockDraft> Blocks { get; } = new();
    }

    private abstract class ReportBlockDraft
    {
        public string Id { get; set; } = MakeReportId("block");
    }

    private sealed class TextReportBlockDraft : ReportBlockDraft
    {
        public string Content { get; set; } = string.Empty;
        public string Mode { get; set; } = "paragraph";
    }

    private sealed class TableReportBlockDraft : ReportBlockDraft
    {
        public string Title { get; set; } = string.Empty;
        public bool HasHeaderRow { get; set; } = true;
        public List<TableRowDraft> Rows { get; } = new();
    }

    private sealed class TableRowDraft
    {
        public string Id { get; set; } = MakeReportId("row");
        public List<TableCellDraft> Cells { get; set; } = new();
    }

    private sealed class TableCellDraft
    {
        public string Id { get; set; } = MakeReportId("cell");
        public string Text { get; set; } = string.Empty;
        public int Colspan { get; set; } = 1;
        public int Rowspan { get; set; } = 1;
        public bool Hidden { get; set; }
    }

    private sealed class ImageReportBlockDraft : ReportBlockDraft
    {
        public string Title { get; set; } = string.Empty;
        public int? AttachmentId { get; set; }
        public string? UploadClientId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Alt { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string? Base64Content { get; set; }
        public string? LocalPath { get; set; }
    }

    private sealed class TechnicalValues
    {
        public string ComputerName { get; set; } = string.Empty;
        public Dictionary<string, string> Characteristics { get; } = new(StringComparer.Ordinal);
    }
}
