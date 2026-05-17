using System.Globalization;
using System.Net;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PracticeMonitoring.Web.Models.DepartmentStaff;
using PracticeMonitoring.Web.Models.Student;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PracticeMonitoring.Web.Services;

public class AttestationSheetService
{
    private const string DocumentFontName = "Times New Roman";
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");
    private static string SignatureDateLine => $"Дата: «___» ______________ {DateTime.Now.Year} г.";

    public string BuildPreviewHtml(DepartmentStaffPracticeDetailsViewModel practice)
    {
        return BuildPreviewHtml(AttestationSheetData.FromDepartmentPractice(practice));
    }

    public string BuildPreviewHtml(StudentPracticeDetailsViewModel practice)
    {
        return BuildPreviewHtml(AttestationSheetData.FromStudentPractice(practice));
    }

    public byte[] BuildDocx(DepartmentStaffPracticeDetailsViewModel practice)
    {
        return BuildDocx(AttestationSheetData.FromDepartmentPractice(practice));
    }

    public byte[] BuildDocx(StudentPracticeDetailsViewModel practice)
    {
        return BuildDocx(AttestationSheetData.FromStudentPractice(practice));
    }

    public byte[] BuildPdf(StudentPracticeDetailsViewModel practice)
    {
        return BuildPdf(AttestationSheetData.FromStudentPractice(practice));
    }

    public string BuildFileName(DepartmentStaffPracticeDetailsViewModel practice)
    {
        return $"Аттестационный_лист_{SafeFilePart(StripAcademicPrefix(practice.PracticeIndex, "ПП"), "practice")}.docx";
    }

    public string BuildFileName(StudentPracticeDetailsViewModel practice)
    {
        var student = SafeFilePart(practice.StudentFullName, "student");
        var practiceIndex = SafeFilePart(StripAcademicPrefix(practice.PracticeIndex, "ПП"), "practice");

        return $"Аттестационный_лист_{practiceIndex}_{student}.docx";
    }

    public string BuildPdfFileName(StudentPracticeDetailsViewModel practice)
    {
        return Path.ChangeExtension(BuildFileName(practice), ".pdf");
    }

    public string BuildArchiveFileName(StudentPracticeDetailsViewModel practice)
    {
        return Path.ChangeExtension(BuildFileName(practice), ".zip");
    }

    public List<AttestationSheetValidationItem> Validate(StudentPracticeDetailsViewModel practice)
    {
        var missing = new List<AttestationSheetValidationItem>();

        AddIfEmpty(missing, "overview", nameof(practice.StudentFullName), practice.StudentFullName, "В профиле студента не указано полное ФИО.");
        AddIfEmpty(missing, "overview", nameof(practice.StudentGroup), practice.StudentGroup, "У студента не указана группа.");

        if (!practice.StudentCourse.HasValue)
            missing.Add(new AttestationSheetValidationItem("overview", nameof(practice.StudentCourse), "У группы студента не указан курс."));

        AddIfEmpty(missing, "overview", nameof(practice.SupervisorFullName), practice.SupervisorFullName, "Руководитель практики от техникума не назначен.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationFullName), FirstNotEmpty(practice.OrganizationFullName, practice.OrganizationName), "Укажите организацию прохождения практики.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorFullName), practice.OrganizationSupervisorFullName, "Укажите ФИО руководителя от профильной организации.");
        AddIfEmpty(missing, "organization", nameof(practice.OrganizationSupervisorPosition), practice.OrganizationSupervisorPosition, "Укажите должность руководителя от профильной организации.");

        return missing;
    }

    private static string BuildPreviewHtml(AttestationSheetData data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<div class='attestation-preview-sheet'>");
        sb.AppendLine("<div class='attestation-preview-header'>");
        sb.AppendLine("<div>МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ</div>");
        sb.AppendLine("<div>федеральное государственное бюджетное образовательное учреждение высшего образования «Российский экономический университет имени Г.В. Плеханова»</div>");
        sb.AppendLine("<div>______________________________________________________________________________________</div>");
        sb.AppendLine("<div>Московский приборостроительный техникум</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='attestation-preview-title'>АТТЕСТАЦИОННЫЙ ЛИСТ</div>");
        sb.AppendLine("<div class='attestation-preview-subtitle'>(характеристика профессиональной деятельности студента во время производственной практики)</div>");

        sb.AppendLine($"<div class='attestation-preview-line'>{Html(BlankIfEmpty(data.StudentFullName, "________________________________________________"))}</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Фамилия, Имя, Отчество</div>");

        sb.AppendLine("<p class='attestation-preview-paragraph'>");
        sb.AppendLine($"обучающийся на {Html(BlankIfEmpty(data.StudentCourse, "_____"))} курсе в группе {Html(BlankIfEmpty(data.StudentGroup, "__________________"))} по специальности СПО ");
        sb.AppendLine($"{Html(data.SpecialtyCode)} «{Html(data.SpecialtyName)}» ");
        sb.AppendLine($"успешно прошел(ла) производственную практику ПП {Html(data.PracticeIndex)} «{Html(data.Name)}» ");
        sb.AppendLine($"по профессиональному модулю ПМ {Html(data.ProfessionalModuleCode)} «{Html(data.ProfessionalModuleName)}» ");
        sb.AppendLine($"в объеме {data.Hours} часов в период: с {FormatDate(data.StartDate)} по {FormatDate(data.EndDate)}.");
        sb.AppendLine("</p>");

        sb.AppendLine("<div class='attestation-preview-section-title'>Виды, объём и качество выполненных работ обучающимся во время практики</div>");

        sb.AppendLine("<table class='attestation-preview-table'>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Виды работ</th>");
        sb.AppendLine("<th>Объём выполненных работ (часов)</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var competency in data.Competencies)
        {
            var workTypesHtml = Html(competency.WorkTypes).Replace("\n", "<br />").Replace("\r", "");

            sb.AppendLine("<tr>");
            sb.AppendLine("<td>");
            sb.AppendLine($"<div class='attestation-preview-pc'>{Html(BuildCompetencyTitle(competency))}</div>");
            sb.AppendLine($"<div class='attestation-preview-worktypes'>{workTypesHtml}</div>");
            sb.AppendLine("</td>");
            sb.AppendLine($"<td class='attestation-preview-hours'>{competency.Hours}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr>");
        sb.AppendLine("<td><strong>Итого часов</strong></td>");
        sb.AppendLine($"<td class='attestation-preview-hours'><strong>{data.Hours}</strong></td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class='attestation-preview-quality'>");
        sb.AppendLine("<div>Качество выполнения работ в соответствии с требованиями программы практики:</div>");
        sb.AppendLine("<div class='attestation-preview-line'>_________________ (__________________)</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>оценка цифрой &nbsp;&nbsp;&nbsp;&nbsp; (оценка прописью)</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='attestation-preview-signatures'>");
        sb.AppendLine("<div class='attestation-preview-sign-block'>");
        sb.AppendLine("<div class='attestation-preview-sign-title'>База прохождения производственной практики</div>");
        sb.AppendLine($"<div>Предприятие (организация): {Html(BlankIfEmpty(data.OrganizationName, "_______________________________________________________"))}</div>");
        sb.AppendLine("<div>Руководитель практической подготовки от профильной организации</div>");
        sb.AppendLine("<div class='attestation-preview-sign-grid'>");
        sb.AppendLine($"<div>{Html(BlankIfEmpty(data.OrganizationSupervisorPosition, "________________"))}</div><div>{Html(BlankIfEmpty(data.OrganizationSupervisorFullName, "_________________"))}</div><div>______________</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Должность</div><div class='attestation-preview-line-caption'>ФИО</div><div class='attestation-preview-line-caption'>Подпись</div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class='attestation-preview-date'>{SignatureDateLine}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='attestation-preview-sign-block'>");
        sb.AppendLine("<div>Итоговая оценка по практике _________________________ (____________________)</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>оценка цифрой &nbsp;&nbsp;&nbsp;&nbsp; (оценка прописью)</div>");
        sb.AppendLine("<div>Руководитель практической подготовки от Московского приборостроительного техникума</div>");
        sb.AppendLine("<div class='attestation-preview-sign-grid'>");
        sb.AppendLine($"<div>___________</div><div>{Html(BlankIfEmpty(data.TechnicalSupervisorFullName, "___________"))}</div><div>______________</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Должность</div><div class='attestation-preview-line-caption'>ФИО</div><div class='attestation-preview-line-caption'>Подпись</div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class='attestation-preview-date'>{SignatureDateLine}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        return sb.ToString();
    }

    private static byte[] BuildDocx(AttestationSheetData data)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            AddDefaultStyles(mainPart);
            var body = new Body();

            body.Append(CreateCenteredParagraph("МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ", true, 24));
            body.Append(CreateCenteredParagraph("федеральное государственное бюджетное образовательное учреждение высшего образования «Российский экономический университет имени Г.В. Плеханова»", false, 22));
            body.Append(CreateCenteredParagraph("______________________________________________________________________________________", false, 22));
            body.Append(CreateCenteredParagraph("Московский приборостроительный техникум", false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateCenteredParagraph("АТТЕСТАЦИОННЫЙ ЛИСТ", true, 28));
            body.Append(CreateCenteredParagraph("(характеристика профессиональной деятельности студента во время производственной практики)", false, 22));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph(BlankIfEmpty(data.StudentFullName, "________________________________________________"), false, 24));
            body.Append(CreateLeftParagraph("Фамилия, Имя, Отчество", false, 20));
            body.Append(CreateEmptyParagraph());

            var introText =
                $"обучающийся на {BlankIfEmpty(data.StudentCourse, "_____")} курсе в группе {BlankIfEmpty(data.StudentGroup, "__________________")} по специальности СПО {data.SpecialtyCode} «{data.SpecialtyName}» " +
                $"успешно прошел(ла) производственную практику ПП {data.PracticeIndex} «{data.Name}» " +
                $"по профессиональному модулю ПМ {data.ProfessionalModuleCode} «{data.ProfessionalModuleName}» " +
                $"в объеме {data.Hours} часов в период: с {FormatDate(data.StartDate)} по {FormatDate(data.EndDate)}.";

            body.Append(CreateJustifiedParagraph(introText, false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Виды, объём и качество выполненных работ обучающимся во время практики", true, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateCompetenciesTable(data));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Качество выполнения работ в соответствии с требованиями программы практики:", false, 24));
            body.Append(CreateLeftParagraph("_________________ (__________________)", false, 24));
            body.Append(CreateLeftParagraph("оценка цифрой           (оценка прописью)", false, 20));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("База прохождения производственной практики", true, 24));
            body.Append(CreateLeftParagraph($"Предприятие (организация): {BlankIfEmpty(data.OrganizationName, "_______________________________________________________")}", false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Руководитель практической подготовки от профильной организации", false, 24));
            body.Append(CreateLeftParagraph($"{BlankIfEmpty(data.OrganizationSupervisorPosition, "________________")}    {BlankIfEmpty(data.OrganizationSupervisorFullName, "_________________")}    ______________", false, 24));
            body.Append(CreateLeftParagraph("Должность           ФИО                 Подпись", false, 20));
            body.Append(CreateLeftParagraph(SignatureDateLine, false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Итоговая оценка по практике _________________________ (____________________)", false, 24));
            body.Append(CreateLeftParagraph("оценка цифрой                           (оценка прописью)", false, 20));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Руководитель практической подготовки от Московского приборостроительного техникума", false, 24));
            body.Append(CreateLeftParagraph($"___________    {BlankIfEmpty(data.TechnicalSupervisorFullName, "___________")}    ______________", false, 24));
            body.Append(CreateLeftParagraph("Должность      ФИО            Подпись", false, 20));
            body.Append(CreateLeftParagraph(SignatureDateLine, false, 24));
            body.Append(CreateSectionProperties());

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] BuildPdf(AttestationSheetData data)
    {
        return QuestPDF.Fluent.Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Millimetre);
                page.DefaultTextStyle(text => text
                    .FontFamily(DocumentFontName)
                    .FontSize(11)
                    .LineHeight(1.25f));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().AlignCenter().Text("МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ").Bold().FontSize(11);
                    column.Item().AlignCenter().Text("федеральное государственное бюджетное образовательное учреждение высшего образования «Российский экономический университет имени Г.В. Плеханова»").FontSize(10);
                    column.Item().AlignCenter().Text("______________________________________________________________________________________").FontSize(10);
                    column.Item().AlignCenter().Text("Московский приборостроительный техникум").FontSize(11);

                    column.Item().PaddingTop(14).AlignCenter().Text("АТТЕСТАЦИОННЫЙ ЛИСТ").Bold().FontSize(16);
                    column.Item().AlignCenter().Text("(характеристика профессиональной деятельности студента во время производственной практики)").FontSize(10);

                    column.Item().PaddingTop(10).Text(BlankIfEmpty(data.StudentFullName, "________________________________________________")).FontSize(11);
                    column.Item().Text("Фамилия, Имя, Отчество").FontSize(8);

                    column.Item().PaddingTop(8).Text(text =>
                    {
                        text.Line($"обучающийся на {BlankIfEmpty(data.StudentCourse, "_____")} курсе в группе {BlankIfEmpty(data.StudentGroup, "__________________")} по специальности СПО");
                        text.Line($"{data.SpecialtyCode} «{data.SpecialtyName}» успешно прошел(ла) производственную практику ПП {data.PracticeIndex}");
                        text.Line($"«{data.Name}» по профессиональному модулю ПМ {data.ProfessionalModuleCode} «{data.ProfessionalModuleName}»");
                        text.Line($"в объеме {data.Hours} часов в период: с {FormatDate(data.StartDate)} по {FormatDate(data.EndDate)}.");
                    });

                    column.Item().PaddingTop(8).Text("Виды, объём и качество выполненных работ обучающимся во время практики").Bold();
                    column.Item().Element(container => BuildPdfCompetenciesTable(container, data));

                    column.Item().PaddingTop(8).Text("Качество выполнения работ в соответствии с требованиями программы практики:");
                    column.Item().Text("_________________ (__________________)");
                    column.Item().Text("оценка цифрой           (оценка прописью)").FontSize(8);

                    column.Item().PaddingTop(8).Text("База прохождения производственной практики").Bold();
                    column.Item().Text($"Предприятие (организация): {BlankIfEmpty(data.OrganizationName, "_______________________________________________________")}");
                    column.Item().Text("Руководитель практической подготовки от профильной организации");
                    column.Item().Element(container => BuildPdfSignatureGrid(
                        container,
                        BlankIfEmpty(data.OrganizationSupervisorPosition, "________________"),
                        BlankIfEmpty(data.OrganizationSupervisorFullName, "_________________"),
                        "______________"));
                    column.Item().Text(SignatureDateLine);

                    column.Item().PaddingTop(8).Text("Итоговая оценка по практике _________________________ (____________________)");
                    column.Item().Text("оценка цифрой                           (оценка прописью)").FontSize(8);

                    column.Item().Text("Руководитель практической подготовки от Московского приборостроительного техникума");
                    column.Item().Element(container => BuildPdfSignatureGrid(
                        container,
                        "___________",
                        BlankIfEmpty(data.TechnicalSupervisorFullName, "___________"),
                        "______________"));
                    column.Item().Text(SignatureDateLine);
                });
            });
        }).GeneratePdf();
    }

    private static void BuildPdfCompetenciesTable(IContainer container, AttestationSheetData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.35f);
            });

            table.Header(header =>
            {
                header.Cell().Element(PdfTableHeaderCell).AlignCenter().Text("Виды работ").Bold();
                header.Cell().Element(PdfTableHeaderCell).AlignCenter().Text("Объём выполненных работ (часов)").Bold();
            });

            foreach (var competency in data.Competencies)
            {
                table.Cell().Element(PdfTableCell).Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text(BuildCompetencyTitle(competency)).Bold();
                    foreach (var line in SplitLines(competency.WorkTypes))
                        column.Item().Text(line);
                });
                table.Cell().Element(PdfTableCell).AlignMiddle().AlignCenter().Text(competency.Hours.ToString(CultureInfo.InvariantCulture));
            }

            table.Cell().Element(PdfTableCell).Text("Итого часов").Bold();
            table.Cell().Element(PdfTableCell).AlignCenter().Text(data.Hours.ToString(CultureInfo.InvariantCulture)).Bold();
        });
    }

    private static void BuildPdfSignatureGrid(IContainer container, string position, string fullName, string signature)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().PaddingTop(6).Text(position);
            table.Cell().PaddingTop(6).Text(fullName);
            table.Cell().PaddingTop(6).Text(signature);
            table.Cell().Text("Должность").FontSize(8);
            table.Cell().Text("ФИО").FontSize(8);
            table.Cell().Text("Подпись").FontSize(8);
        });
    }

    private static IContainer PdfTableHeaderCell(IContainer container)
    {
        return container.Border(0.6f).Padding(6).MinHeight(34);
    }

    private static IContainer PdfTableCell(IContainer container)
    {
        return container.Border(0.6f).Padding(6);
    }

    private static Table CreateCompetenciesTable(AttestationSheetData data)
    {
        var table = new Table();

        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 8 },
                new BottomBorder { Val = BorderValues.Single, Size = 8 },
                new LeftBorder { Val = BorderValues.Single, Size = 8 },
                new RightBorder { Val = BorderValues.Single, Size = 8 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 8 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 8 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

        table.AppendChild(tableProperties);

        table.Append(CreateTableRow(
            CreateTableCell("Виды работ", true, JustificationValues.Center),
            CreateTableCell("Объём выполненных работ (часов)", true, JustificationValues.Center)));

        foreach (var competency in data.Competencies)
        {
            var text = $"{BuildCompetencyTitle(competency)}\n{competency.WorkTypes}";
            table.Append(CreateTableRow(
                CreateTableCell(text, false, JustificationValues.Left),
                CreateTableCell(competency.Hours.ToString(CultureInfo.InvariantCulture), false, JustificationValues.Center)));
        }

        table.Append(CreateTableRow(
            CreateTableCell("Итого часов", true, JustificationValues.Left),
            CreateTableCell(data.Hours.ToString(CultureInfo.InvariantCulture), true, JustificationValues.Center)));

        return table;
    }

    private static TableRow CreateTableRow(params TableCell[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
            row.Append(cell);
        return row;
    }

    private static TableCell CreateTableCell(string text, bool bold, JustificationValues justification)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));

        foreach (var line in text.Split('\n'))
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = justification }),
                new Run(CreateRunProperties(24, bold), new Text(line) { Space = SpaceProcessingModeValues.Preserve }));

            cell.Append(paragraph);
        }

        return cell;
    }

    private static Paragraph CreateCenteredParagraph(string text, bool bold, int halfPoints)
    {
        return CreateParagraph(text, JustificationValues.Center, bold, halfPoints);
    }

    private static Paragraph CreateLeftParagraph(string text, bool bold, int halfPoints)
    {
        return CreateParagraph(text, JustificationValues.Left, bold, halfPoints);
    }

    private static Paragraph CreateJustifiedParagraph(string text, bool bold, int halfPoints)
    {
        return CreateParagraph(text, JustificationValues.Both, bold, halfPoints);
    }

    private static Paragraph CreateParagraph(string text, JustificationValues justification, bool bold, int halfPoints)
    {
        return new Paragraph(
            new ParagraphProperties(new Justification { Val = justification }),
            new Run(CreateRunProperties(halfPoints, bold), new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreateEmptyParagraph()
    {
        return CreateLeftParagraph(" ", false, 24);
    }

    private static RunProperties CreateRunProperties(int halfPoints, bool bold)
    {
        var fontSize = halfPoints.ToString(CultureInfo.InvariantCulture);
        var properties = new RunProperties(
            new RunFonts
            {
                Ascii = DocumentFontName,
                HighAnsi = DocumentFontName,
                EastAsia = DocumentFontName,
                ComplexScript = DocumentFontName
            },
            new FontSize { Val = fontSize },
            new FontSizeComplexScript { Val = fontSize });

        if (bold)
            properties.Append(new Bold(), new BoldComplexScript());

        return properties;
    }

    private static void AddDefaultStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts
                        {
                            Ascii = DocumentFontName,
                            HighAnsi = DocumentFontName,
                            EastAsia = DocumentFontName,
                            ComplexScript = DocumentFontName
                        },
                        new FontSize { Val = "24" },
                        new FontSizeComplexScript { Val = "24" })),
                new ParagraphPropertiesDefault(
                    new ParagraphPropertiesBaseStyle(
                        new SpacingBetweenLines { Line = "240", LineRule = LineSpacingRuleValues.Auto }))));
        stylesPart.Styles.Save();
    }

    private static SectionProperties CreateSectionProperties()
    {
        return new SectionProperties(
            new DocumentFormat.OpenXml.Wordprocessing.PageSize { Width = 11906, Height = 16838 },
            new PageMargin
            {
                Top = 1134,
                Right = 1134U,
                Bottom = 1134,
                Left = 1134U,
                Header = 708U,
                Footer = 708U,
                Gutter = 0U
            });
    }

    private static string BuildCompetencyTitle(AttestationSheetCompetencyData competency)
    {
        var code = competency.CompetencyCode?.Trim() ?? string.Empty;

        if (code.StartsWith("ПК", StringComparison.OrdinalIgnoreCase))
            return $"{code} «{competency.CompetencyDescription}»";

        return $"ПК {code} «{competency.CompetencyDescription}»";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("dd.MM.yyyy", RuCulture);
    }

    private static string BlankIfEmpty(string? value, string blank)
    {
        return string.IsNullOrWhiteSpace(value) ? blank : value.Trim();
    }

    private static string StripAcademicPrefix(string? value, string prefix)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
            return normalized;

        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[prefix.Length..].TrimStart();
            if (normalized.StartsWith(".", StringComparison.Ordinal))
                normalized = normalized[1..].TrimStart();
        }

        return normalized;
    }

    private static IEnumerable<string> SplitLines(string? value)
    {
        var lines = (value ?? string.Empty).Replace("\r", string.Empty).Split('\n');
        return lines.Length == 0 ? new[] { string.Empty } : lines;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static void AddIfEmpty(List<AttestationSheetValidationItem> items, string tab, string field, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            items.Add(new AttestationSheetValidationItem(tab, field, message));
    }

    private static string SafeFilePart(string? value, string fallback)
    {
        var normalized = string.Concat((value ?? string.Empty)
            .Trim()
            .Select(ch => char.IsWhiteSpace(ch) ? '_' : ch)
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_'));

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private sealed class AttestationSheetData
    {
        public string PracticeIndex { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string SpecialtyCode { get; init; } = string.Empty;

        public string SpecialtyName { get; init; } = string.Empty;

        public string ProfessionalModuleCode { get; init; } = string.Empty;

        public string ProfessionalModuleName { get; init; } = string.Empty;

        public int Hours { get; init; }

        public DateTime StartDate { get; init; }

        public DateTime EndDate { get; init; }

        public string? StudentFullName { get; init; }

        public string? StudentGroup { get; init; }

        public string? StudentCourse { get; init; }

        public string? OrganizationName { get; init; }

        public string? OrganizationSupervisorFullName { get; init; }

        public string? OrganizationSupervisorPosition { get; init; }

        public string? TechnicalSupervisorFullName { get; init; }

        public List<AttestationSheetCompetencyData> Competencies { get; init; } = new();

        public static AttestationSheetData FromDepartmentPractice(DepartmentStaffPracticeDetailsViewModel practice)
        {
            return new AttestationSheetData
            {
                PracticeIndex = StripAcademicPrefix(practice.PracticeIndex, "ПП"),
                Name = practice.Name,
                SpecialtyCode = practice.SpecialtyCode,
                SpecialtyName = practice.SpecialtyName,
                ProfessionalModuleCode = StripAcademicPrefix(practice.ProfessionalModuleCode, "ПМ"),
                ProfessionalModuleName = practice.ProfessionalModuleName,
                Hours = practice.Hours,
                StartDate = practice.StartDate,
                EndDate = practice.EndDate,
                Competencies = practice.Competencies
                    .Select(x => new AttestationSheetCompetencyData
                    {
                        CompetencyCode = x.CompetencyCode,
                        CompetencyDescription = x.CompetencyDescription,
                        WorkTypes = x.WorkTypes,
                        Hours = x.Hours
                    })
                    .ToList()
            };
        }

        public static AttestationSheetData FromStudentPractice(StudentPracticeDetailsViewModel practice)
        {
            return new AttestationSheetData
            {
                PracticeIndex = StripAcademicPrefix(practice.PracticeIndex, "ПП"),
                Name = practice.Name,
                SpecialtyCode = practice.SpecialtyCode,
                SpecialtyName = practice.SpecialtyName,
                ProfessionalModuleCode = StripAcademicPrefix(practice.ProfessionalModuleCode, "ПМ"),
                ProfessionalModuleName = practice.ProfessionalModuleName,
                Hours = practice.Hours,
                StartDate = practice.StartDate,
                EndDate = practice.EndDate,
                StudentFullName = practice.StudentFullName,
                StudentGroup = practice.StudentGroup,
                StudentCourse = practice.StudentCourse?.ToString(CultureInfo.InvariantCulture),
                OrganizationName = FirstNotEmpty(practice.OrganizationFullName, practice.OrganizationName),
                OrganizationSupervisorFullName = practice.OrganizationSupervisorFullName,
                OrganizationSupervisorPosition = practice.OrganizationSupervisorPosition,
                TechnicalSupervisorFullName = practice.SupervisorFullName,
                Competencies = practice.Competencies
                    .Select(x => new AttestationSheetCompetencyData
                    {
                        CompetencyCode = x.CompetencyCode,
                        CompetencyDescription = x.CompetencyDescription,
                        WorkTypes = x.WorkTypes,
                        Hours = x.Hours
                    })
                    .ToList()
            };
        }
    }

    private sealed class AttestationSheetCompetencyData
    {
        public string CompetencyCode { get; init; } = string.Empty;

        public string CompetencyDescription { get; init; } = string.Empty;

        public string WorkTypes { get; init; } = string.Empty;

        public int Hours { get; init; }
    }
}

public sealed record AttestationSheetValidationItem(string Tab, string Field, string Message);
