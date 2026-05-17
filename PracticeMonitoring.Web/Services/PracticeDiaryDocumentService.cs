using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PracticeMonitoring.Web.Models.Student;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PracticeMonitoring.Web.Services;

public class PracticeDiaryDocumentService
{
    private const string TemplateFileName = "PracticeDiary_Template.docx";
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly IWebHostEnvironment _environment;

    public PracticeDiaryDocumentService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<PracticeDiaryBuildResult> BuildDocxAsync(StudentPracticeDetailsViewModel practice)
    {
        var validation = Validate(practice);
        if (validation.Count > 0)
            return PracticeDiaryBuildResult.Failed(validation);

        var templatePath = ResolveTemplatePath();
        if (!File.Exists(templatePath))
        {
            return PracticeDiaryBuildResult.Failed(new[]
            {
                new PracticeDiaryValidationItem("documents", "Template", "Шаблон дневника практики не найден.")
            });
        }

        await using var templateStream = File.OpenRead(templatePath);
        using var output = new MemoryStream();
        await templateStream.CopyToAsync(output);
        output.Position = 0;

        using (var document = WordprocessingDocument.Open(output, true))
        {
            var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("DOCX template has no main part.");
            var wordDocument = mainPart.Document ?? throw new InvalidOperationException("DOCX template has no document.");
            var body = wordDocument.Body ?? throw new InvalidOperationException("DOCX template has no body.");

            ApplyPracticeData(body, practice);
            ReplaceGeneralCompetencies(body, practice);
            ReplaceProfessionalCompetencies(body, practice);
            ReplaceDiaryTable(body, practice);

            wordDocument.Save();
        }

        return PracticeDiaryBuildResult.Ok(output.ToArray(), BuildFileName(practice));
    }

    public List<PracticeDiaryValidationItem> Validate(StudentPracticeDetailsViewModel practice)
    {
        var missing = new List<PracticeDiaryValidationItem>();

        AddIfEmpty(missing, "overview", nameof(practice.StudentFullName), practice.StudentFullName, "В профиле студента не указано полное ФИО.");
        AddIfEmpty(missing, "overview", nameof(practice.StudentGroup), practice.StudentGroup, "У студента не указана группа.");

        if (!practice.StudentCourse.HasValue)
            missing.Add(new PracticeDiaryValidationItem("overview", nameof(practice.StudentCourse), "У группы студента не указан курс."));

        AddIfEmpty(missing, "overview", nameof(practice.SupervisorFullName), practice.SupervisorFullName, "Руководитель практики от техникума не назначен.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationFullName), FirstNotEmpty(practice.OrganizationFullName, practice.OrganizationName), "Укажите профильную организацию-базу практики.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationAddress), practice.OrganizationAddress, "Укажите адрес профильной организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorFullName), practice.OrganizationSupervisorFullName, "Укажите ФИО руководителя от профильной организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorPosition), practice.OrganizationSupervisorPosition, "Укажите должность руководителя от профильной организации.");
        AddIfEmpty(missing, "organization", nameof(practice.PracticeTaskContent), practice.PracticeTaskContent, "Укажите содержание задания на практику.");

        if (practice.GeneralCompetencies.Count == 0)
            missing.Add(new PracticeDiaryValidationItem("overview", "GeneralCompetencies", "Добавьте общие компетенции в карточке производственной практики."));

        if (practice.Competencies.Count == 0)
            missing.Add(new PracticeDiaryValidationItem("overview", "Competencies", "Добавьте профессиональные компетенции в карточке производственной практики."));

        ValidateDiaryCompleteness(practice, missing);

        return missing;
    }

    public string BuildFileName(StudentPracticeDetailsViewModel practice)
    {
        var student = SafeFilePart(practice.StudentFullName, "student");
        var practiceIndex = SafeFilePart(practice.PracticeIndex, "practice");

        return $"Дневник_практики_{practiceIndex}_{student}.docx";
    }

    public string BuildPdfFileName(StudentPracticeDetailsViewModel practice)
    {
        return Path.ChangeExtension(BuildFileName(practice), ".pdf");
    }

    public byte[] BuildPdf(StudentPracticeDetailsViewModel practice)
    {
        var entriesByDate = practice.DiaryEntries
            .GroupBy(x => x.WorkDate.Date)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(entry => entry.UpdatedAtUtc).First());

        return QuestPDF.Fluent.Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(16, Unit.Millimetre);
                page.DefaultTextStyle(text => text
                    .FontFamily("Times New Roman")
                    .FontSize(11)
                    .LineHeight(1.2f));

                page.Content().Column(column =>
                {
                    column.Spacing(8);

                    column.Item().AlignCenter().Text("МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ").Bold().FontSize(10);
                    column.Item().AlignCenter().Text("федеральное государственное бюджетное образовательное учреждение высшего образования «Российский экономический университет имени Г.В. Плеханова»").FontSize(9);
                    column.Item().AlignCenter().Text("МОСКОВСКИЙ ПРИБОРОСТРОИТЕЛЬНЫЙ ТЕХНИКУМ").Bold().FontSize(11);

                    column.Item().PaddingTop(10).AlignCenter().Text("Дневник производственной практики").Bold().FontSize(18);
                    column.Item().AlignCenter().Text(BuildPracticeTitle(practice)).FontSize(13);
                    column.Item().AlignCenter().Text(BuildProfessionalModuleTitle(practice)).FontSize(12);

                    column.Item().PaddingTop(8).Text($"Студент {practice.StudentCourse?.ToString(CultureInfo.InvariantCulture) ?? "__"} курса");
                    column.Item().Text($"Специальности {practice.SpecialtyCode} «{practice.SpecialtyName}»");
                    column.Item().Text($"Квалификация: «{practice.QualificationName}»");
                    column.Item().Text($"Группа {practice.StudentGroup}");
                    column.Item().Text($"ФИО студента: {practice.StudentFullName}");
                    column.Item().Text(BuildPracticePeriod(practice.StartDate, practice.EndDate));

                    column.Item().PaddingTop(8).Text("СВЕДЕНИЯ О БАЗЕ ПРАКТИКИ").Bold().FontSize(13);
                    column.Item().Text($"Руководитель практической подготовки от техникума: {practice.SupervisorFullName}");
                    column.Item().Text($"Руководитель практической подготовки от профильной организации: {practice.OrganizationSupervisorFullName}");
                    column.Item().Text($"Должность: {practice.OrganizationSupervisorPosition}");
                    column.Item().Text($"Профильная организация-база практики: {FirstNotEmpty(practice.OrganizationFullName, practice.OrganizationName)}");
                    column.Item().Text($"Адрес профильной организации с почтовым индексом: {practice.OrganizationAddress}");

                    column.Item().PaddingTop(8).Text("ЦЕЛИ И ЗАДАЧИ ПРАКТИКИ").Bold().FontSize(13);
                    column.Item().Text(BuildPracticeGoal(practice));

                    column.Item().PaddingTop(6).Text("Общие компетенции:").Bold();
                    foreach (var competency in practice.GeneralCompetencies.OrderBy(x => x.SortOrder))
                        column.Item().Text($"- {BuildCompetencyLine(competency.CompetencyCode, competency.CompetencyDescription)}");

                    column.Item().PaddingTop(6).Text(BuildProfessionalCompetenciesIntro(practice)).Bold();
                    foreach (var competency in practice.Competencies)
                        column.Item().Text($"- {BuildCompetencyLine(competency.CompetencyCode, competency.CompetencyDescription)}");

                    column.Item().PaddingTop(8).Text("СОДЕРЖАНИЕ ЗАДАНИЯ НА ПРАКТИКУ").Bold().FontSize(13);
                    column.Item().Text(practice.PracticeTaskContent ?? string.Empty);

                    column.Item().PaddingTop(8).Text("ДНЕВНИК ПРАКТИКИ").Bold().FontSize(13);
                    column.Item().Element(container => BuildPdfDiaryTable(container, practice, entriesByDate));

                    column.Item().PaddingTop(8).Text("Характеристика студента").Bold();
                    column.Item().Text("____________________________________________________________________________________________");
                    column.Item().Text("Оценка работы студента за практику _____________ (_________________)");
                    column.Item().Text($"Руководитель практической подготовки от профильной организации ______________________ /{BuildShortName(practice.OrganizationSupervisorFullName)}/");

                    column.Item().PaddingTop(6).Text("Заключение руководителя практической подготовки от техникума и оценка результатов практики").Bold();
                    column.Item().Text("____________________________________________________________________________________________");
                    column.Item().Text("Итоговая оценка по практике ________________________________");
                    column.Item().Text($"Руководитель практической подготовки от техникума ______________ {practice.SupervisorFullName}");
                });
            });
        }).GeneratePdf();
    }

    private static void BuildPdfDiaryTable(
        IContainer container,
        StudentPracticeDetailsViewModel practice,
        Dictionary<DateTime, StudentPracticeDiaryEntryViewModel> entriesByDate)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.25f);
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.6f);
            });

            table.Header(header =>
            {
                header.Cell().Element(PdfTableHeaderCell).AlignCenter().Text("Дата или период выполнения работ").Bold();
                header.Cell().Element(PdfTableHeaderCell).AlignCenter().Text("Краткое содержание выполняемых работ").Bold();
                header.Cell().Element(PdfTableHeaderCell).AlignCenter().Text("Подпись руководителя").Bold();
            });

            foreach (var workDate in GetWorkDays(practice.StartDate, practice.EndDate))
            {
                entriesByDate.TryGetValue(workDate.Date, out var entry);
                table.Cell().Element(PdfTableCell).AlignCenter().Text(FormatDate(workDate));
                table.Cell().Element(PdfTableCell).Text(entry?.ShortDescription ?? string.Empty);
                table.Cell().Element(PdfTableCell).Text(string.Empty);
            }
        });
    }

    private static IContainer PdfTableHeaderCell(IContainer container)
    {
        return container.Border(0.6f).Padding(5).MinHeight(28);
    }

    private static IContainer PdfTableCell(IContainer container)
    {
        return container.Border(0.6f).Padding(5).MinHeight(26);
    }

    private string ResolveTemplatePath()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "templates", TemplateFileName),
            Path.Combine(_environment.ContentRootPath, "Templates", TemplateFileName)
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static void ApplyPracticeData(Body body, StudentPracticeDetailsViewModel practice)
    {
        var studentName = SplitStudentName(practice.StudentFullName);
        var organizationName = FirstNotEmpty(practice.OrganizationFullName, practice.OrganizationName) ?? string.Empty;

        SetFirstParagraphStartingWith(body, "ПП.[Индекс ПП]", BuildPracticeTitle(practice));
        SetFirstParagraphStartingWith(body, "ПМ[Код профессионального модуля]", BuildProfessionalModuleTitle(practice));
        SetFirstParagraphStartingWith(body, "Студент __ курса", $"Студент {practice.StudentCourse?.ToString(CultureInfo.InvariantCulture) ?? "__"} курса");
        SetFirstParagraphStartingWith(body, "Специальности [Номер специальности]", $"Специальности {practice.SpecialtyCode} «{practice.SpecialtyName}»");
        SetFirstParagraphStartingWith(body, "Квалификация:", $"Квалификация: «{practice.QualificationName}»");
        SetFirstParagraphStartingWith(body, "Группа [Группа студента]", $"Группа {practice.StudentGroup}");
        SetFirstParagraphStartingWith(body, "Фамилия ", $"Фамилия {studentName.Surname}");
        SetFirstParagraphStartingWith(body, "Имя ", $"Имя {studentName.FirstName}");
        SetFirstParagraphStartingWith(body, "Отчество ", $"Отчество {studentName.Patronymic}");
        SetFirstParagraphStartingWith(body, "- с «[День начала]»", BuildPracticePeriod(practice.StartDate, practice.EndDate));

        SetParagraphStartingWithByOccurrence(body, "Ф.И.О.:", 0, $"Ф.И.О.:  {practice.SupervisorFullName}");
        SetParagraphStartingWithByOccurrence(body, "Ф.И.О.:", 1, $"Ф.И.О.: {practice.OrganizationSupervisorFullName}");
        SetParagraphStartingWithByOccurrence(body, "Должность:", 1, $"Должность: {practice.OrganizationSupervisorPosition}");

        SetNextParagraphAfter(body, "Профильная организация-база практики:", organizationName);
        SetNextParagraphsAfter(body, "Адрес профильной организации с почтовым индексом:", new[] { practice.OrganizationAddress ?? string.Empty, " " });

        SetFirstParagraphStartingWith(body, "Практика имеет целью", BuildPracticeGoal(practice));
        SetFirstParagraphStartingWith(body, "СОДЕРЖАНИЕ ЗАДАНИЯ НА ПРАКТИКУ", "СОДЕРЖАНИЕ ЗАДАНИЯ НА ПРАКТИКУ");
        SetNextParagraphAfter(body, "СОДЕРЖАНИЕ ЗАДАНИЯ НА ПРАКТИКУ", practice.PracticeTaskContent ?? string.Empty);

        SetFirstParagraphContaining(body, "________ 2026 год", $"«__» ________ {practice.EndDate.Year.ToString(CultureInfo.InvariantCulture)} год\u00A0");
        SetFirstParagraphStartingWith(body, "При прохождении практики студент", $"При прохождении практики студент {BuildShortName(practice.StudentFullName)}");
        SetFirstParagraphStartingWith(body, "______________________ /", $"______________________ /{BuildShortName(practice.OrganizationSupervisorFullName)}/");
        SetFirstParagraphStartingWith(body, "______________ __________________", $"______________ {practice.SupervisorFullName}");
    }

    private static void ReplaceGeneralCompetencies(Body body, StudentPracticeDetailsViewModel practice)
    {
        var header = FindParagraphStartingWith(body, "Общие компетенции:");
        var end = FindParagraphStartingWith(body, "В результате изучения профессионального модуля");

        if (header is null || end is null)
            return;

        var template = ElementsBetween(header, end).OfType<Paragraph>().FirstOrDefault();
        var paragraphs = practice.GeneralCompetencies
            .OrderBy(x => x.SortOrder)
            .Select(x => CloneParagraphWithText(template, $"- {BuildCompetencyLine(x.CompetencyCode, x.CompetencyDescription)}"))
            .ToList();

        ReplaceElementsBetween(header, end, paragraphs);
    }

    private static void ReplaceProfessionalCompetencies(Body body, StudentPracticeDetailsViewModel practice)
    {
        var intro = FindParagraphStartingWith(body, "В результате изучения профессионального модуля");
        if (intro is not null)
            SetParagraphText(intro, BuildProfessionalCompetenciesIntro(practice));

        var end = FindParagraphStartingWith(body, "ИНСТРУКТАЖ ПО ТЕХНИКЕ БЕЗОПАСНОСТИ");
        if (intro is null || end is null)
            return;

        var template = ElementsBetween(intro, end).OfType<Paragraph>().FirstOrDefault();
        var paragraphs = practice.Competencies
            .Select(x => CloneParagraphWithText(template, $"- {BuildCompetencyLine(x.CompetencyCode, x.CompetencyDescription)}"))
            .ToList();

        ReplaceElementsBetween(intro, end, paragraphs);
    }

    private static void ReplaceDiaryTable(Body body, StudentPracticeDetailsViewModel practice)
    {
        var table = body.Descendants<Table>()
            .FirstOrDefault(x => GetText(x).Contains("Дата или период выполнения", StringComparison.OrdinalIgnoreCase));

        if (table is null)
            return;

        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0)
            return;

        var templateRow = rows.Count > 1
            ? (TableRow)rows[1].CloneNode(true)
            : (TableRow)rows[0].CloneNode(true);

        foreach (var row in rows.Skip(1))
            row.Remove();

        var entriesByDate = practice.DiaryEntries
            .GroupBy(x => x.WorkDate.Date)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(entry => entry.UpdatedAtUtc).First());

        foreach (var workDate in GetWorkDays(practice.StartDate, practice.EndDate))
        {
            var row = (TableRow)templateRow.CloneNode(true);
            var cells = row.Elements<TableCell>().ToList();

            if (cells.Count >= 3)
            {
                SetCellText(cells[0], FormatDate(workDate));
                SetCellText(cells[1], entriesByDate.TryGetValue(workDate.Date, out var entry) ? entry.ShortDescription : string.Empty);
                SetCellText(cells[2], string.Empty);
            }

            table.Append(row);
        }
    }

    private static void ValidateDiaryCompleteness(StudentPracticeDetailsViewModel practice, List<PracticeDiaryValidationItem> missing)
    {
        var entries = practice.DiaryEntries
            .GroupBy(x => x.WorkDate.Date)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.UpdatedAtUtc).First());

        foreach (var workDate in GetWorkDays(practice.StartDate, practice.EndDate))
        {
            if (!entries.TryGetValue(workDate.Date, out var entry) || string.IsNullOrWhiteSpace(entry.ShortDescription))
                missing.Add(new PracticeDiaryValidationItem("diary", workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), $"Заполните краткую запись дневника за {FormatDate(workDate)}."));
        }
    }

    private static void SetFirstParagraphStartingWith(Body body, string prefix, string value)
    {
        SetParagraphStartingWithByOccurrence(body, prefix, 0, value);
    }

    private static void SetParagraphStartingWithByOccurrence(Body body, string prefix, int occurrence, string value)
    {
        var paragraph = body.Descendants<Paragraph>()
            .Where(x => GetText(x).TrimStart().StartsWith(prefix, StringComparison.Ordinal))
            .Skip(occurrence)
            .FirstOrDefault();

        if (paragraph is not null)
            SetParagraphText(paragraph, value);
    }

    private static void SetFirstParagraphContaining(Body body, string valuePart, string value)
    {
        var paragraph = body.Descendants<Paragraph>()
            .FirstOrDefault(x => GetText(x).Contains(valuePart, StringComparison.Ordinal));

        if (paragraph is not null)
            SetParagraphText(paragraph, value);
    }

    private static void SetNextParagraphAfter(Body body, string prefix, string value)
    {
        SetNextParagraphsAfter(body, prefix, new[] { value });
    }

    private static void SetNextParagraphsAfter(Body body, string prefix, IReadOnlyList<string> values)
    {
        var start = FindParagraphStartingWith(body, prefix);
        if (start is null)
            return;

        var targets = start.ElementsAfter()
            .OfType<Paragraph>()
            .Where(x => !string.IsNullOrWhiteSpace(GetText(x)))
            .Take(values.Count)
            .ToList();

        for (var i = 0; i < targets.Count; i++)
            SetParagraphText(targets[i], values[i]);
    }

    private static Paragraph? FindParagraphStartingWith(Body body, string prefix)
    {
        return body.Descendants<Paragraph>()
            .FirstOrDefault(x => GetText(x).TrimStart().StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IEnumerable<OpenXmlElement> ElementsBetween(OpenXmlElement startExclusive, OpenXmlElement endExclusive)
    {
        var current = startExclusive.NextSibling();
        while (current is not null && current != endExclusive)
        {
            yield return current;
            current = current.NextSibling();
        }
    }

    private static void ReplaceElementsBetween(OpenXmlElement startExclusive, OpenXmlElement endExclusive, IEnumerable<OpenXmlElement> replacements)
    {
        foreach (var element in ElementsBetween(startExclusive, endExclusive).ToList())
            element.Remove();

        foreach (var replacement in replacements)
            endExclusive.InsertBeforeSelf(replacement);
    }

    private static Paragraph CloneParagraphWithText(Paragraph? template, string text)
    {
        var paragraph = template is null
            ? new Paragraph()
            : (Paragraph)template.CloneNode(true);

        SetParagraphText(paragraph, text);
        return paragraph;
    }

    private static void SetParagraphText(Paragraph paragraph, string value)
    {
        var paragraphProperties = paragraph.ParagraphProperties?.CloneNode(true);
        var runProperties = paragraph.Descendants<RunProperties>().FirstOrDefault()?.CloneNode(true);

        paragraph.RemoveAllChildren();

        if (paragraphProperties is not null)
            paragraph.Append(paragraphProperties);

        var lines = SplitLines(value).ToList();
        var run = new Run();
        if (runProperties is not null)
            run.Append(runProperties);

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                run.Append(new Break());

            run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }

        paragraph.Append(run);
    }

    private static void SetCellText(TableCell cell, string value)
    {
        var templateParagraph = cell.Elements<Paragraph>().FirstOrDefault();

        foreach (var paragraph in cell.Elements<Paragraph>().ToList())
            paragraph.Remove();

        foreach (var line in SplitLines(value))
        {
            var paragraph = templateParagraph is null
                ? new Paragraph()
                : (Paragraph)templateParagraph.CloneNode(true);

            SetParagraphText(paragraph, line);
            cell.Append(paragraph);
        }
    }

    private static string BuildPracticeTitle(StudentPracticeDetailsViewModel practice)
    {
        return $"{BuildPracticeIndex(practice.PracticeIndex)} {practice.Name}".Trim();
    }

    private static string BuildProfessionalModuleTitle(StudentPracticeDetailsViewModel practice)
    {
        return $"{BuildProfessionalModuleCode(practice.ProfessionalModuleCode)} {practice.ProfessionalModuleName}".Trim();
    }

    private static string BuildPracticeGoal(StudentPracticeDetailsViewModel practice)
    {
        return $"Практика имеет целью комплексное освоение студентами всех видов профессиональной деятельности по специальности {practice.SpecialtyCode} «{practice.SpecialtyName}» Квалификация «{practice.QualificationName}», формирование общих и профессиональных компетенций, а также приобретение необходимых умений и опыта практической работы по специальности.";
    }

    private static string BuildProfessionalCompetenciesIntro(StudentPracticeDetailsViewModel practice)
    {
        return $"В результате изучения профессионального модуля {BuildProfessionalModuleCode(practice.ProfessionalModuleCode)} «{practice.ProfessionalModuleName}» студент должен обладать профессиональными компетенциями, соответствующими основным видам профессиональной деятельности:";
    }

    private static string BuildCompetencyLine(string code, string description)
    {
        var trimmedCode = code.Trim();
        var trimmedDescription = description.Trim();

        if (string.IsNullOrWhiteSpace(trimmedCode))
            return trimmedDescription;

        return trimmedDescription.StartsWith(trimmedCode, StringComparison.OrdinalIgnoreCase)
            ? trimmedDescription
            : $"{trimmedCode}. {trimmedDescription}";
    }

    private static string BuildPracticePeriod(DateTime startDate, DateTime endDate)
    {
        return $"- с «{startDate:dd}» {FormatMonthGenitive(startDate)} {startDate:yyyy} года по «{endDate:dd}» {FormatMonthGenitive(endDate)} {endDate:yyyy} года";
    }

    private static string BuildPracticeIndex(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("ПП", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return $"ПП.{trimmed}";
    }

    private static string BuildProfessionalModuleCode(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("ПМ", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return $"ПМ.{trimmed}";
    }

    private static StudentNameParts SplitStudentName(string? fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new StudentNameParts(
            parts.ElementAtOrDefault(0) ?? string.Empty,
            parts.ElementAtOrDefault(1) ?? string.Empty,
            string.Join(' ', parts.Skip(2)));
    }

    private static string BuildShortName(string? fullName)
    {
        var parts = SplitStudentName(fullName);
        var initials = string.Concat(new[] { parts.FirstName, parts.Patronymic }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => $"{x[0]}."));

        return string.IsNullOrWhiteSpace(parts.Surname)
            ? fullName?.Trim() ?? string.Empty
            : $"{parts.Surname} {initials}".Trim();
    }

    private static string FormatMonthGenitive(DateTime date)
    {
        return date.Month switch
        {
            1 => "января",
            2 => "февраля",
            3 => "марта",
            4 => "апреля",
            5 => "мая",
            6 => "июня",
            7 => "июля",
            8 => "августа",
            9 => "сентября",
            10 => "октября",
            11 => "ноября",
            12 => "декабря",
            _ => date.ToString("MMMM", RuCulture)
        };
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("dd.MM.yyyy", RuCulture);
    }

    private static IEnumerable<DateTime> GetWorkDays(DateTime startDate, DateTime endDate)
    {
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                yield return date;
        }
    }

    private static IEnumerable<string> SplitLines(string? value)
    {
        var lines = (value ?? string.Empty).Replace("\r", string.Empty).Split('\n');
        return lines.Length == 0 ? new[] { string.Empty } : lines;
    }

    private static string GetText(OpenXmlElement element)
    {
        return string.Concat(element.Descendants<Text>().Select(x => x.Text));
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static void AddIfEmpty(List<PracticeDiaryValidationItem> items, string tab, string field, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            items.Add(new PracticeDiaryValidationItem(tab, field, message));
    }

    private static string SafeFilePart(string? value, string fallback)
    {
        var normalized = string.Concat((value ?? string.Empty)
            .Trim()
            .Select(ch => char.IsWhiteSpace(ch) ? '_' : ch)
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_'));

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private sealed record StudentNameParts(string Surname, string FirstName, string Patronymic);
}

public sealed record PracticeDiaryValidationItem(string Tab, string Field, string Message);

public class PracticeDiaryBuildResult
{
    public bool Success { get; init; }

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public string FileName { get; init; } = "practice-diary.docx";

    public List<PracticeDiaryValidationItem> Missing { get; init; } = new();

    public static PracticeDiaryBuildResult Ok(byte[] content, string fileName)
    {
        return new PracticeDiaryBuildResult { Success = true, Content = content, FileName = fileName };
    }

    public static PracticeDiaryBuildResult Failed(IEnumerable<PracticeDiaryValidationItem> missing)
    {
        return new PracticeDiaryBuildResult { Success = false, Missing = missing.ToList() };
    }
}
