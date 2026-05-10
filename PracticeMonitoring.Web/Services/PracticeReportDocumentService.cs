using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using PracticeMonitoring.Web.Models.Student;

namespace PracticeMonitoring.Web.Services;

public class PracticeReportDocumentService
{
    private const string TemplateFileName = "PracticeReport_Template_ForProgram.docx";
    private const string TechnicalComputerLabel = "Компьютер";
    private static readonly string[] TechnicalCharacteristicLabels =
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
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IWebHostEnvironment _environment;

    public PracticeReportDocumentService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<PracticeReportBuildResult> BuildDocxAsync(
        StudentPracticeDetailsViewModel practice,
        Func<int, Task<StudentFileResult?>> attachmentResolver,
        Func<int, Task<StudentFileResult?>> appendixResolver)
    {
        var validation = Validate(practice);
        if (validation.Count > 0)
            return PracticeReportBuildResult.Failed(validation);

        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", TemplateFileName);
        if (!File.Exists(templatePath))
        {
            return PracticeReportBuildResult.Failed(new[]
            {
                new PracticeReportValidationItem("documents", "Template", "Шаблон отчёта не найден.")
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

            ReplaceSimplePlaceholders(body, BuildSimplePlaceholderMap(practice));
            ReplaceBlockPlaceholder(body, "TABLE_OF_CONTENTS", StaticTableOfContents());
            ReplaceBlockPlaceholder(body, "INTRO_WORK_TYPES_LIST", BuildList(GetItems(practice, "IntroductionWorkType")));
            ReplaceBlockPlaceholder(body, "INTRO_SOFTWARE_TECHNOLOGIES_LIST", BuildList(GetItems(practice, "IntroductionSoftwareTechnology")));
            ReplaceBlockPlaceholder(body, "GENERAL_COMPETENCIES_LIST", BuildGeneralCompetencies(practice));
            ReplaceBlockPlaceholder(body, "PROFESSIONAL_COMPETENCIES_LIST", BuildProfessionalCompetencies(practice));
            ReplaceBlockPlaceholder(body, "TECHNICAL_TOOLS_TABLE", new OpenXmlElement[] { BuildTechnicalToolsTable(practice) });
            ReplaceBlockPlaceholder(body, "SOFTWARE_TOOLS_TABLE", new OpenXmlElement[] { BuildSoftwareToolsTable(practice) });
            ReplaceBlockPlaceholder(body, "DAILY_WORKS_TABLE", new OpenXmlElement[] { BuildDailyWorksTable(practice) });
            ReplaceBlockPlaceholder(body, "SOURCES_LIST", BuildSources(practice));

            var counters = new ReportCounters();
            var dailyBlocks = await BuildDailyContentBlocksAsync(practice, mainPart, attachmentResolver, counters);
            ReplaceBlockPlaceholder(body, "REPORT_DAILY_CONTENT_BLOCKS", dailyBlocks);
            var appendixBlocks = await BuildAppendicesAsync(practice, mainPart, appendixResolver);
            ReplaceBlockPlaceholder(body, "APPENDICES_BLOCKS", appendixBlocks);

            AddCenteredPageNumberFooter(mainPart);
            NormalizeGeneratedDocument(body);

            wordDocument.Save();
        }

        return PracticeReportBuildResult.Ok(output.ToArray(), BuildFileName(practice));
    }

    public string BuildPreviewHtml(StudentPracticeDetailsViewModel practice)
    {
        var values = BuildSimplePlaceholderMap(practice);
        var sb = new StringBuilder();

        sb.AppendLine("<div class='student-practice-report-preview'>");
        sb.AppendLine("<div class='student-practice-report-preview-title'>Отчёт по производственной практике</div>");
        sb.AppendLine("<div class='student-practice-report-preview-meta'>");
        AppendPreviewRow(sb, "Студент", values["STUDENT_FULL_NAME"]);
        AppendPreviewRow(sb, "Группа", values["STUDENT_GROUP"]);
        AppendPreviewRow(sb, "Специальность", $"{values["SPECIALTY_CODE"]} {values["SPECIALTY_NAME"]}".Trim());
        AppendPreviewRow(sb, "Квалификация", values["QUALIFICATION_NAME"]);
        AppendPreviewRow(sb, "Практика", $"{values["PRACTICE_INDEX"]} {values["PRACTICE_NAME"]}".Trim());
        AppendPreviewRow(sb, "Профессиональный модуль", $"{values["PROFESSIONAL_MODULE_CODE"]} {values["PROFESSIONAL_MODULE_NAME"]}".Trim());
        AppendPreviewRow(sb, "Организация", values["ORGANIZATION_FULL_NAME"]);
        AppendPreviewRow(sb, "Руководитель от организации", values["ORGANIZATION_SUPERVISOR_FULL_NAME"]);
        AppendPreviewRow(sb, "Руководитель от техникума", values["TECHNICAL_SUPERVISOR_FULL_NAME"]);
        AppendPreviewRow(sb, "Год подписания", values["SIGN_YEAR"]);
        sb.AppendLine("</div>");

        AppendPreviewSection(sb, "Введение", values["INTRO_MAIN_GOAL"]);
        AppendPreviewList(sb, "Виды работ", GetItems(practice, "IntroductionWorkType"));
        AppendPreviewList(sb, "Программные средства и технологии", GetItems(practice, "IntroductionSoftwareTechnology"));
        AppendPreviewSection(sb, "Выполняемые обязанности", values["STUDENT_DUTIES"]);
        AppendPreviewSection(sb, "Задание на практику", values["PRACTICE_TASK_CONTENT"]);
        AppendPreviewSection(sb, "Предоставленные материалы", values["PROVIDED_MATERIALS_DESCRIPTION"]);
        AppendPreviewSection(sb, "График работы", values["WORK_SCHEDULE_DESCRIPTION"]);
        AppendPreviewList(sb, "Общие компетенции", practice.GeneralCompetencies.OrderBy(x => x.SortOrder).Select(x => $"{x.CompetencyCode}. {x.CompetencyDescription}"));
        AppendPreviewList(sb, "Профессиональные компетенции", practice.Competencies.Select(x => $"{x.CompetencyCode}. {x.CompetencyDescription}"));
        AppendPreviewList(sb, "Источники", practice.Sources.Select((x, i) => $"{i + 1}. {x.Title}{(string.IsNullOrWhiteSpace(x.Url) ? "" : $" - {x.Url}")}"));
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    public List<PracticeReportValidationItem> Validate(StudentPracticeDetailsViewModel practice)
    {
        var missing = new List<PracticeReportValidationItem>();

        AddIfEmpty(missing, "organization", nameof(practice.OrganizationFullName), practice.OrganizationFullName ?? practice.OrganizationName, "Укажите полное название организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationShortName), practice.OrganizationShortName, "Укажите сокращенное название организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationAddress), practice.OrganizationAddress, "Укажите адрес организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorFullName), practice.OrganizationSupervisorFullName, "Укажите ФИО руководителя от организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorPosition), practice.OrganizationSupervisorPosition, "Укажите должность руководителя от организации.");
        AddIfEmpty(missing, "introduction", nameof(practice.IntroductionMainGoal), practice.IntroductionMainGoal, "Укажите основную цель практики.");
        AddIfEmpty(missing, "diary", nameof(practice.StudentDuties), practice.StudentDuties, "Укажите выполняемые обязанности.");
        AddIfEmpty(missing, "diary", nameof(practice.ProvidedMaterialsDescription), practice.ProvidedMaterialsDescription, "Опишите предоставленные материалы.");
        AddIfEmpty(missing, "diary", nameof(practice.WorkScheduleDescription), practice.WorkScheduleDescription, "Опишите график работы.");

        if (practice.GeneralCompetencies.Count == 0)
            missing.Add(new PracticeReportValidationItem("overview", "GeneralCompetencies", "Добавьте общие компетенции в карточке производственной практики."));

        if (practice.Competencies.Count == 0)
            missing.Add(new PracticeReportValidationItem("overview", "Competencies", "Добавьте профессиональные компетенции."));

        if (!GetItems(practice, "IntroductionWorkType").Any())
            missing.Add(new PracticeReportValidationItem("introduction", "IntroductionWorkType", "Добавьте хотя бы один вид работ во введении."));

        if (!GetItems(practice, "IntroductionSoftwareTechnology").Any())
            missing.Add(new PracticeReportValidationItem("introduction", "IntroductionSoftwareTechnology", "Добавьте хотя бы одно программное средство или технологию во введении."));

        if (!practice.ReportItems.Any(x => x.Category == "TechnicalTool"))
            missing.Add(new PracticeReportValidationItem("technicalTools", "TechnicalTool", "Добавьте технические средства."));

        if (!practice.DiaryEntries.Any(x => !string.IsNullOrWhiteSpace(x.ShortDescription)))
            missing.Add(new PracticeReportValidationItem("diary", "DiaryEntries", "Заполните краткие записи дневника."));

        foreach (var entry in practice.DiaryEntries)
            ValidateDetailedReport(entry, missing);

        return missing;
    }

    private static void ValidateDetailedReport(StudentPracticeDiaryEntryViewModel entry, List<PracticeReportValidationItem> missing)
    {
        if (string.IsNullOrWhiteSpace(entry.DetailedReport))
            return;

        DayReportDocument? report;
        try
        {
            report = JsonSerializer.Deserialize<DayReportDocument>(entry.DetailedReport, JsonOptions);
        }
        catch
        {
            missing.Add(new PracticeReportValidationItem("diary", entry.WorkDate.ToString("yyyy-MM-dd"), $"Некорректный JSON подробного отчёта за {FormatDate(entry.WorkDate)}."));
            return;
        }

        foreach (var block in report?.Blocks ?? new List<JsonElement>())
        {
            var type = GetString(block, "type");
            if (type is "table")
            {
                if (string.IsNullOrWhiteSpace(GetString(block, "title")))
                    missing.Add(new PracticeReportValidationItem("diary", "DetailedReport", $"У таблицы в отчёте за {FormatDate(entry.WorkDate)} нет подписи."));
            }
            else if (type is "image" or "figure")
            {
                if (string.IsNullOrWhiteSpace(GetString(block, "title")))
                    missing.Add(new PracticeReportValidationItem("diary", "DetailedReport", $"У рисунка в отчёте за {FormatDate(entry.WorkDate)} нет подписи."));

                if (!GetInt(block, "attachmentId").HasValue)
                    missing.Add(new PracticeReportValidationItem("diary", "DetailedReport", $"У рисунка в отчёте за {FormatDate(entry.WorkDate)} нет attachmentId."));
            }
        }
    }

    private static Dictionary<string, string> BuildSimplePlaceholderMap(StudentPracticeDetailsViewModel practice)
    {
        return new Dictionary<string, string>
        {
            ["PRACTICE_INDEX"] = practice.PracticeIndex,
            ["PRACTICE_NAME"] = practice.Name,
            ["PROFESSIONAL_MODULE_CODE"] = practice.ProfessionalModuleCode,
            ["PROFESSIONAL_MODULE_NAME"] = practice.ProfessionalModuleName,
            ["SPECIALTY_CODE"] = practice.SpecialtyCode,
            ["SPECIALTY_NAME"] = practice.SpecialtyName,
            ["QUALIFICATION_NAME"] = practice.QualificationName,
            ["STUDENT_FULL_NAME"] = practice.StudentFullName,
            ["STUDENT_GROUP"] = practice.StudentGroup,
            ["STUDENT_COURSE"] = practice.StudentCourse?.ToString(CultureInfo.InvariantCulture) ?? "",
            ["TECHNICAL_SUPERVISOR_FULL_NAME"] = practice.SupervisorFullName ?? "",
            ["ORGANIZATION_FULL_NAME"] = practice.OrganizationFullName ?? practice.OrganizationName ?? "",
            ["ORGANIZATION_SHORT_NAME"] = practice.OrganizationShortName ?? "",
            ["ORGANIZATION_ADDRESS"] = practice.OrganizationAddress ?? "",
            ["ORGANIZATION_SUPERVISOR_FULL_NAME"] = practice.OrganizationSupervisorFullName ?? "",
            ["ORGANIZATION_SUPERVISOR_POSITION"] = practice.OrganizationSupervisorPosition ?? "",
            ["SIGN_YEAR"] = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture),
            ["INTRO_MAIN_GOAL"] = practice.IntroductionMainGoal ?? "",
            ["STUDENT_DUTIES"] = practice.StudentDuties ?? "",
            ["PRACTICE_TASK_CONTENT"] = practice.PracticeTaskContent ?? "",
            ["PROVIDED_MATERIALS_DESCRIPTION"] = practice.ProvidedMaterialsDescription ?? "",
            ["WORK_SCHEDULE_DESCRIPTION"] = practice.WorkScheduleDescription ?? ""
        };
    }

    private static void ReplaceSimplePlaceholders(Body body, Dictionary<string, string> values)
    {
        foreach (var paragraph in body.Descendants<Paragraph>().ToList())
        {
            var original = paragraph.InnerText;
            var replaced = original;
            foreach (var item in values)
            {
                replaced = replaced.Replace($"[[{item.Key}]]", item.Value ?? string.Empty, StringComparison.Ordinal);
            }

            if (replaced == original)
                continue;

            var firstRunProperties = paragraph.Descendants<RunProperties>().FirstOrDefault()?.CloneNode(true);
            paragraph.RemoveAllChildren<Run>();

            var run = new Run();
            if (firstRunProperties is not null)
                run.Append(firstRunProperties);
            run.Append(new Text(replaced) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);
        }
    }

    private static void ReplaceBlockPlaceholder(Body body, string placeholder, IEnumerable<OpenXmlElement> replacement)
    {
        var token = $"[[{placeholder}]]";
        foreach (var paragraph in body.Descendants<Paragraph>().Where(p => p.InnerText.Contains(token, StringComparison.Ordinal)).ToList())
        {
            foreach (var element in replacement.Reverse())
                paragraph.InsertAfterSelf(element.CloneNode(true));

            paragraph.Remove();
        }
    }

    private static IEnumerable<OpenXmlElement> StaticTableOfContents()
    {
        return new OpenXmlElement[]
        {
            TableOfContentsLine("ВВЕДЕНИЕ", "3"),
            TableOfContentsLine("1. ЦЕЛИ И ЗАДАЧИ ПРАКТИКИ", "4"),
            TableOfContentsLine("2. БАЗА ПРАКТИКИ", "7"),
            TableOfContentsLine("3. СОДЕРЖАНИЕ ПРАКТИКИ", "9"),
            TableOfContentsLine("ВЫВОД", "11"),
            TableOfContentsLine("СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ", "12")
        };
    }

    private static IEnumerable<OpenXmlElement> BuildList(IEnumerable<string> items)
    {
        var list = items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Paragraph($"- {x.Trim()}")).Cast<OpenXmlElement>().ToList();
        return list.Count == 0 ? new OpenXmlElement[] { Paragraph("-") } : list;
    }

    private static IEnumerable<OpenXmlElement> BuildGeneralCompetencies(StudentPracticeDetailsViewModel practice)
    {
        return practice.GeneralCompetencies
            .OrderBy(x => x.SortOrder)
            .Select(x => Paragraph($"- {x.CompetencyCode}. {x.CompetencyDescription};"))
            .Cast<OpenXmlElement>()
            .ToList();
    }

    private static IEnumerable<OpenXmlElement> BuildProfessionalCompetencies(StudentPracticeDetailsViewModel practice)
    {
        return practice.Competencies
            .Select(x => Paragraph($"{x.CompetencyCode}. {x.CompetencyDescription}."))
            .Cast<OpenXmlElement>()
            .ToList();
    }

    private static Table BuildTechnicalToolsTable(StudentPracticeDetailsViewModel practice)
    {
        var rows = practice.ReportItems
            .Where(x => x.Category == "TechnicalTool")
            .OrderBy(x => x.SortOrder)
            .ToList();

        var computerName = rows.FirstOrDefault(x => x.Name == TechnicalComputerLabel)?.Description;
        if (string.IsNullOrWhiteSpace(computerName))
            computerName = rows.FirstOrDefault(x => !TechnicalCharacteristicLabels.Contains(x.Name))?.Name;

        var values = rows
            .Where(x => TechnicalCharacteristicLabels.Contains(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Description ?? "");

        var table = CreateTable("№", "Тип оборудования", "Наименование и характеристики");
        table.Append(Row(Cell(""), Cell("2"), Cell("3")));
        table.Append(Row(SpannedCell(computerName ?? "", 3, true)));

        for (var i = 0; i < TechnicalCharacteristicLabels.Length; i++)
        {
            var label = TechnicalCharacteristicLabels[i];
            table.Append(Row(
                Cell((i + 1).ToString(CultureInfo.InvariantCulture)),
                Cell(label),
                Cell(values.TryGetValue(label, out var value) ? value : "")));
        }

        return table;
    }

    private static Table BuildSoftwareToolsTable(StudentPracticeDetailsViewModel practice)
    {
        var rows = practice.ReportItems.Where(x => x.Category == "SoftwareTool").ToList();
        if (rows.Count == 0)
            rows = practice.ReportItems.Where(x => x.Category == "IntroductionSoftwareTechnology").ToList();

        var table = CreateTable("№", "Тип средства", "Название средства", "Назначение");
        for (var i = 0; i < rows.Count; i++)
        {
            table.Append(Row(
                Cell((i + 1).ToString(CultureInfo.InvariantCulture)),
                Cell(rows[i].Name),
                Cell(rows[i].Name),
                Cell(rows[i].Description ?? "")));
        }

        return table;
    }

    private static Table BuildDailyWorksTable(StudentPracticeDetailsViewModel practice)
    {
        var table = CreateTable("Дата", "Виды работ, выполненные на практике");
        foreach (var entry in practice.DiaryEntries.OrderBy(x => x.WorkDate).Where(x => !string.IsNullOrWhiteSpace(x.ShortDescription)))
            table.Append(Row(Cell(FormatDate(entry.WorkDate)), Cell(entry.ShortDescription)));

        return table;
    }

    private static IEnumerable<OpenXmlElement> BuildSources(StudentPracticeDetailsViewModel practice)
    {
        return practice.Sources.Count == 0
            ? new OpenXmlElement[] { Paragraph("Список источников не заполнен.") }
            : practice.Sources.Select((x, i) => Paragraph($"{i + 1}. {x.Title}{(string.IsNullOrWhiteSpace(x.Url) ? "" : $" - {x.Url}")}")).Cast<OpenXmlElement>();
    }

    private static async Task<IEnumerable<OpenXmlElement>> BuildAppendicesAsync(
        StudentPracticeDetailsViewModel practice,
        MainDocumentPart mainPart,
        Func<int, Task<StudentFileResult?>> appendixResolver)
    {
        if (practice.Appendices.Count == 0)
            return new OpenXmlElement[] { Paragraph("Приложения отсутствуют.") };

        var output = new List<OpenXmlElement>();
        foreach (var appendix in practice.Appendices.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id))
        {
            var file = await appendixResolver(appendix.Id);
            if (file is null || file.Content.Length == 0)
                continue;

            if (output.Count > 0)
                output.Add(PageBreakParagraph());

            output.AddRange(BuildAppendixContent(mainPart, file));
        }

        return output.Count == 0
            ? new OpenXmlElement[] { Paragraph("Приложения отсутствуют.") }
            : output;
    }

    private static IEnumerable<OpenXmlElement> BuildAppendixContent(MainDocumentPart mainPart, StudentFileResult file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();

        if (extension is ".docx" || contentType.Contains("wordprocessingml.document", StringComparison.Ordinal))
            return new OpenXmlElement[] { CreateWordAltChunk(mainPart, file.Content) };

        if (extension is ".txt" or ".cs" or ".sql" || contentType.StartsWith("text/", StringComparison.Ordinal))
            return SplitLines(DecodeText(file.Content)).Select(line => Paragraph(line)).Cast<OpenXmlElement>().ToList();

        if (extension is ".png" or ".jpg" or ".jpeg" || contentType.StartsWith("image/", StringComparison.Ordinal))
            return new OpenXmlElement[] { CreateImageParagraph(mainPart, file.Content, file.ContentType, $"appendix-{Guid.NewGuid():N}") };

        return new OpenXmlElement[] { Paragraph($"Содержимое файла {file.FileName} нельзя вставить в DOCX автоматически.") };
    }

    private static AltChunk CreateWordAltChunk(MainDocumentPart mainPart, byte[] content)
    {
        var part = mainPart.AddAlternativeFormatImportPart(
            AlternativeFormatImportPartType.WordprocessingML,
            $"AltChunkId{Guid.NewGuid():N}");
        using (var stream = new MemoryStream(content))
            part.FeedData(stream);

        return new AltChunk { Id = mainPart.GetIdOfPart(part) };
    }

    private static string DecodeText(byte[] content)
    {
        try
        {
            return Encoding.UTF8.GetString(content);
        }
        catch
        {
            return Encoding.Default.GetString(content);
        }
    }

    private static async Task<IEnumerable<OpenXmlElement>> BuildDailyContentBlocksAsync(
        StudentPracticeDetailsViewModel practice,
        MainDocumentPart mainPart,
        Func<int, Task<StudentFileResult?>> attachmentResolver,
        ReportCounters counters)
    {
        var output = new List<OpenXmlElement>();

        foreach (var entry in practice.DiaryEntries.OrderBy(x => x.WorkDate))
        {
            var blocks = ParseBlocks(entry.DetailedReport);
            if (blocks.Count == 0)
                continue;

            output.Add(Paragraph(FormatDate(entry.WorkDate), bold: true));

            foreach (var block in blocks)
            {
                var type = GetString(block, "type");
                if (type is "text")
                {
                    foreach (var line in SplitLines(GetString(block, "content")))
                        output.Add(Paragraph(line));
                }
                else if (type is "table")
                {
                    counters.TableNumber++;
                    output.Add(Paragraph($"Таблица {counters.TableNumber} - {GetString(block, "title")}", bold: true));
                    output.Add(BuildReportTable(block));
                }
                else if (type is "image" or "figure")
                {
                    var attachmentId = GetInt(block, "attachmentId");
                    if (!attachmentId.HasValue)
                        continue;

                    var file = await attachmentResolver(attachmentId.Value);
                    if (file is null)
                        continue;

                    counters.FigureNumber++;
                    output.Add(CreateImageParagraph(mainPart, file.Content, file.ContentType, $"practice-figure-{attachmentId.Value}"));
                    output.Add(Paragraph($"Рисунок {counters.FigureNumber} - {GetString(block, "title")}", bold: true, center: true));
                }
            }
        }

        return output.Count == 0 ? new OpenXmlElement[] { Paragraph("Подробные отчёты по дням не заполнены.") } : output;
    }

    private static Table BuildReportTable(JsonElement block)
    {
        var table = CreateTable();
        if (!block.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return table;

        foreach (var rowElement in rows.EnumerateArray())
        {
            var row = new TableRow();
            var cells = rowElement.ValueKind switch
            {
                JsonValueKind.Object when rowElement.TryGetProperty("cells", out var objectCells) &&
                                          objectCells.ValueKind == JsonValueKind.Array => objectCells,
                JsonValueKind.Array => rowElement,
                _ => default
            };

            if (cells.ValueKind == JsonValueKind.Array)
            {
                foreach (var cellElement in cells.EnumerateArray())
                {
                    var cellText = cellElement.ValueKind == JsonValueKind.Object
                        ? GetString(cellElement, "text")
                        : GetScalarText(cellElement);
                    var cell = Cell(cellText);
                    var props = cell.GetFirstChild<TableCellProperties>() ?? cell.PrependChild(new TableCellProperties());
                    var colspan = cellElement.ValueKind == JsonValueKind.Object ? GetInt(cellElement, "colspan") ?? 1 : 1;
                    var rowspan = cellElement.ValueKind == JsonValueKind.Object ? GetInt(cellElement, "rowspan") ?? 1 : 1;
                    if (colspan > 1)
                        props.Append(new GridSpan { Val = colspan });
                    if (rowspan > 1)
                        props.Append(new VerticalMerge { Val = MergedCellValues.Restart });

                    row.Append(cell);
                }
            }

            table.Append(row);
        }

        return table;
    }

    private static string GetScalarText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "Да",
            JsonValueKind.False => "Нет",
            _ => ""
        };
    }

    private static List<JsonElement> ParseBlocks(string? detailedReport)
    {
        if (string.IsNullOrWhiteSpace(detailedReport))
            return new List<JsonElement>();

        try
        {
            using var doc = JsonDocument.Parse(detailedReport);
            if (doc.RootElement.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
                return blocks.EnumerateArray().Select(x => x.Clone()).ToList();
            if (doc.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                return content.EnumerateArray().Select(x => x.Clone()).ToList();
        }
        catch
        {
        }

        return new List<JsonElement>();
    }

    private static IEnumerable<string> GetItems(StudentPracticeDetailsViewModel practice, string category)
    {
        return practice.ReportItems
            .Where(x => x.Category == category)
            .OrderBy(x => x.SortOrder)
            .Select(x => string.IsNullOrWhiteSpace(x.Description) ? x.Name : $"{x.Name} - {x.Description}");
    }

    private static void AppendPreviewRow(StringBuilder sb, string label, string? value)
    {
        sb.AppendLine("<div class='student-practice-report-preview-row'>");
        sb.AppendLine($"<span>{Html(label)}</span>");
        sb.AppendLine($"<strong>{Html(string.IsNullOrWhiteSpace(value) ? "Не заполнено" : value)}</strong>");
        sb.AppendLine("</div>");
    }

    private static void AppendPreviewSection(StringBuilder sb, string title, string? text)
    {
        sb.AppendLine("<section class='student-practice-report-preview-section'>");
        sb.AppendLine($"<h4>{Html(title)}</h4>");
        sb.AppendLine($"<p>{Html(string.IsNullOrWhiteSpace(text) ? "Не заполнено" : text)}</p>");
        sb.AppendLine("</section>");
    }

    private static void AppendPreviewList(StringBuilder sb, string title, IEnumerable<string> items)
    {
        var list = items.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        sb.AppendLine("<section class='student-practice-report-preview-section'>");
        sb.AppendLine($"<h4>{Html(title)}</h4>");
        if (list.Count == 0)
        {
            sb.AppendLine("<p>Не заполнено</p>");
        }
        else
        {
            sb.AppendLine("<ol>");
            foreach (var item in list)
                sb.AppendLine($"<li>{Html(item)}</li>");
            sb.AppendLine("</ol>");
        }
        sb.AppendLine("</section>");
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }

    private static Table CreateTable(params string[] headers)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        if (headers.Length > 0)
            table.Append(Row(headers.Select(x => Cell(x, true)).ToArray()));

        return table;
    }

    private static TableRow Row(params TableCell[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
            row.Append(cell);
        return row;
    }

    private static TableCell Cell(string? text, bool bold = false)
    {
        var cell = new TableCell();
        cell.Append(new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
        foreach (var line in SplitLines(text))
            cell.Append(Paragraph(line, bold, fontSize: "24", lineSpacing: "240"));
        return cell;
    }

    private static TableCell SpannedCell(string? text, int columns, bool bold = false)
    {
        var cell = Cell(text, bold);
        var props = cell.GetFirstChild<TableCellProperties>() ?? cell.PrependChild(new TableCellProperties());
        props.Append(new GridSpan { Val = columns });
        return cell;
    }

    private static Paragraph Paragraph(
        string? text,
        bool bold = false,
        bool center = false,
        string fontSize = "28",
        string lineSpacing = "360")
    {
        var props = new ParagraphProperties(
            new SpacingBetweenLines { Line = lineSpacing, LineRule = LineSpacingRuleValues.Auto },
            new Justification { Val = center ? JustificationValues.Center : JustificationValues.Both });
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", ComplexScript = "Times New Roman" },
            new FontSize { Val = fontSize },
            new FontSizeComplexScript { Val = fontSize });
        if (bold)
            runProps.Append(new Bold());

        return new Paragraph(props, new Run(runProps, new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph TableOfContentsLine(string title, string page)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Left },
                new Tabs(new TabStop { Val = TabStopValues.Right, Leader = TabStopLeaderCharValues.Dot, Position = 9350 })),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", ComplexScript = "Times New Roman" },
                    new FontSize { Val = "28" },
                    new FontSizeComplexScript { Val = "28" }),
                new Text(title) { Space = SpaceProcessingModeValues.Preserve },
                new TabChar(),
                new Text(page) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph PageBreakParagraph()
    {
        return new Paragraph(new Run(new Break { Type = BreakValues.Page }));
    }

    private static void AddCenteredPageNumberFooter(MainDocumentPart mainPart)
    {
        foreach (var footer in mainPart.FooterParts.ToList())
            mainPart.DeletePart(footer);

        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", ComplexScript = "Times New Roman" },
                        new FontSize { Val = "24" },
                        new FontSizeComplexScript { Val = "24" }),
                    new Text("1")),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End })));
        footerPart.Footer.Save();

        var relationshipId = mainPart.GetIdOfPart(footerPart);
        var body = mainPart.Document?.Body;
        if (body is null)
            return;

        var sectionProperties = body.Elements<SectionProperties>().LastOrDefault();
        if (sectionProperties is null)
        {
            sectionProperties = new SectionProperties();
            body.Append(sectionProperties);
        }

        sectionProperties.RemoveAllChildren<FooterReference>();
        sectionProperties.PrependChild(new FooterReference { Type = HeaderFooterValues.Default, Id = relationshipId });
    }

    private static void NormalizeGeneratedDocument(Body body)
    {
        var afterTitlePage = false;
        foreach (var element in body.ChildElements)
        {
            if (afterTitlePage)
            {
                if (element is Paragraph paragraph)
                    NormalizeParagraphAfterTitlePage(paragraph);
                else if (element is Table table)
                    NormalizeTable(table);
            }

            if (ContainsPageBreak(element))
                afterTitlePage = true;
        }
    }

    private static void NormalizeParagraphAfterTitlePage(Paragraph paragraph)
    {
        foreach (var bold in paragraph.Descendants<Bold>().ToList())
            bold.Remove();

        var paragraphProperties = paragraph.ParagraphProperties ?? paragraph.PrependChild(new ParagraphProperties());
        paragraphProperties.ParagraphStyleId?.Remove();
        var isTableOfContentsLine = paragraphProperties.Tabs is not null;
        paragraphProperties.Justification ??= new Justification();
        paragraphProperties.Justification.Val = IsCenteredSectionTitle(paragraph.InnerText)
            ? JustificationValues.Center
            : isTableOfContentsLine
                ? JustificationValues.Left
            : JustificationValues.Both;

        paragraphProperties.SpacingBetweenLines ??= new SpacingBetweenLines();
        paragraphProperties.SpacingBetweenLines.Line = isTableOfContentsLine ? "240" : "360";
        paragraphProperties.SpacingBetweenLines.LineRule = LineSpacingRuleValues.Auto;

        foreach (var run in paragraph.Descendants<Run>())
        {
            var runProperties = run.RunProperties ?? run.PrependChild(new RunProperties());
            runProperties.RunFonts ??= new RunFonts();
            runProperties.RunFonts.Ascii = "Times New Roman";
            runProperties.RunFonts.HighAnsi = "Times New Roman";
            runProperties.RunFonts.ComplexScript = "Times New Roman";
            runProperties.FontSize ??= new FontSize();
            runProperties.FontSize.Val = "28";
            runProperties.FontSizeComplexScript ??= new FontSizeComplexScript();
            runProperties.FontSizeComplexScript.Val = "28";
        }
    }

    private static void NormalizeTable(Table table)
    {
        foreach (var bold in table.Descendants<Bold>().ToList())
            bold.Remove();

        foreach (var paragraph in table.Descendants<Paragraph>())
        {
            var paragraphProperties = paragraph.ParagraphProperties ?? paragraph.PrependChild(new ParagraphProperties());
            paragraphProperties.SpacingBetweenLines ??= new SpacingBetweenLines();
            paragraphProperties.SpacingBetweenLines.Line = "240";
            paragraphProperties.SpacingBetweenLines.LineRule = LineSpacingRuleValues.Auto;

            foreach (var run in paragraph.Descendants<Run>())
            {
                var runProperties = run.RunProperties ?? run.PrependChild(new RunProperties());
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.Ascii = "Times New Roman";
                runProperties.RunFonts.HighAnsi = "Times New Roman";
                runProperties.RunFonts.ComplexScript = "Times New Roman";
                runProperties.FontSize ??= new FontSize();
                runProperties.FontSize.Val = "24";
                runProperties.FontSizeComplexScript ??= new FontSizeComplexScript();
                runProperties.FontSizeComplexScript.Val = "24";
            }
        }
    }

    private static bool IsCenteredSectionTitle(string text)
    {
        var normalized = text.Trim().ToUpperInvariant();
        return normalized is "СОДЕРЖАНИЕ"
            or "ВВЕДЕНИЕ"
            or "1. ЦЕЛИ И ЗАДАЧИ ПРАКТИКИ"
            or "1.ЦЕЛИ И ЗАДАЧИ ПРАКТИКИ"
            or "2. БАЗА ПРАКТИКИ"
            or "2.БАЗА ПРАКТИКИ"
            or "3. СОДЕРЖАНИЕ ПРАКТИКИ"
            or "3.СОДЕРЖАНИЕ ПРАКТИКИ"
            or "ВЫВОД"
            or "СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ"
            or "ПРИЛОЖЕНИЯ";
    }

    private static bool ContainsPageBreak(OpenXmlElement element)
    {
        return element.Descendants<Break>().Any(x => x.Type?.Value == BreakValues.Page);
    }

    private static Paragraph CreateImageParagraph(MainDocumentPart mainPart, byte[] imageBytes, string contentType, string name)
    {
        var imagePartType = contentType.ToLowerInvariant() switch
        {
            "image/png" => ImagePartType.Png,
            "image/webp" => ImagePartType.Gif,
            _ => ImagePartType.Jpeg
        };

        var imagePart = mainPart.AddImagePart(imagePartType);
        using (var stream = new MemoryStream(imageBytes))
            imagePart.FeedData(stream);

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        const long width = 5000000;
        const long height = 3000000;

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = 1U, Name = name },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = name },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = width, Cy = height }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

        return new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(drawing));
    }

    private static IEnumerable<string> SplitLines(string? text)
    {
        var lines = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
        return lines.Length == 0 ? new[] { string.Empty } : lines;
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(property, out var value) &&
               value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? GetInt(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(property, out var value) &&
               value.TryGetInt32(out var number)
            ? number
            : null;
    }

    private static void AddIfEmpty(List<PracticeReportValidationItem> items, string tab, string field, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            items.Add(new PracticeReportValidationItem(tab, field, message));
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("dd.MM.yyyy", RuCulture);
    }

    private static string BuildFileName(StudentPracticeDetailsViewModel practice)
    {
        var safeIndex = string.Concat(practice.PracticeIndex.Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_'));
        return $"Отчет_практики_{(string.IsNullOrWhiteSpace(safeIndex) ? "practice" : safeIndex)}.docx";
    }

    private static string ToRussianAppendixLetter(int index)
    {
        const string letters = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧШЭЮЯ";
        return index >= 0 && index < letters.Length ? letters[index].ToString() : (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private sealed class DayReportDocument
    {
        public List<JsonElement> Blocks { get; set; } = new();
    }

    private sealed class ReportCounters
    {
        public int TableNumber { get; set; }

        public int FigureNumber { get; set; }
    }
}

public sealed record PracticeReportValidationItem(string Tab, string Field, string Message);

public class PracticeReportBuildResult
{
    public bool Success { get; init; }

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public string FileName { get; init; } = "practice-report.docx";

    public List<PracticeReportValidationItem> Missing { get; init; } = new();

    public static PracticeReportBuildResult Ok(byte[] content, string fileName)
    {
        return new PracticeReportBuildResult { Success = true, Content = content, FileName = fileName };
    }

    public static PracticeReportBuildResult Failed(IEnumerable<PracticeReportValidationItem> missing)
    {
        return new PracticeReportBuildResult { Success = false, Missing = missing.ToList() };
    }
}
