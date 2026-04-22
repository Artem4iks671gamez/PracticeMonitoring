using System.Globalization;
using System.Net;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PracticeMonitoring.Web.Models.DepartmentStaff;

namespace PracticeMonitoring.Web.Services;

public class AttestationSheetService
{
    public string BuildPreviewHtml(DepartmentStaffPracticeDetailsViewModel practice)
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

        sb.AppendLine("<div class='attestation-preview-line'>________________________________________________</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Фамилия, Имя, Отчество</div>");

        sb.AppendLine("<p class='attestation-preview-paragraph'>");
        sb.AppendLine("обучающийся на _____ курсе в группе __________________ по специальности СПО ");
        sb.AppendLine($"{Html(practice.SpecialtyCode)} «{Html(practice.SpecialtyName)}» ");
        sb.AppendLine($"успешно прошел(ла) производственную практику ПП {Html(practice.PracticeIndex)} «{Html(practice.Name)}» ");
        sb.AppendLine($"по профессиональному модулю ПМ {Html(practice.ProfessionalModuleCode)} «{Html(practice.ProfessionalModuleName)}» ");
        sb.AppendLine($"в объеме {practice.Hours} часов в период: с {FormatDate(practice.StartDate)} по {FormatDate(practice.EndDate)}.");
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

        foreach (var competency in practice.Competencies)
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
        sb.AppendLine($"<td class='attestation-preview-hours'><strong>{practice.Hours}</strong></td>");
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
        sb.AppendLine("<div>Предприятие (организация): _______________________________________________________</div>");
        sb.AppendLine("<div>Руководитель практической подготовки от профильной организации</div>");
        sb.AppendLine("<div class='attestation-preview-sign-grid'>");
        sb.AppendLine("<div>________________</div><div>_________________</div><div>______________</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Должность</div><div class='attestation-preview-line-caption'>ФИО</div><div class='attestation-preview-line-caption'>Подпись</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='attestation-preview-date'>Дата: «___» ______________ 2025 г.</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='attestation-preview-sign-block'>");
        sb.AppendLine("<div>Итоговая оценка по практике _________________________ (____________________)</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>оценка цифрой &nbsp;&nbsp;&nbsp;&nbsp; (оценка прописью)</div>");
        sb.AppendLine("<div>Руководитель практической подготовки от Московского приборостроительного техникума</div>");
        sb.AppendLine("<div class='attestation-preview-sign-grid'>");
        sb.AppendLine("<div>___________</div><div>___________</div><div>______________</div>");
        sb.AppendLine("<div class='attestation-preview-line-caption'>Должность</div><div class='attestation-preview-line-caption'>ФИО</div><div class='attestation-preview-line-caption'>Подпись</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='attestation-preview-date'>Дата: «___» ______________ 2025 г.</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        return sb.ToString();
    }

    public byte[] BuildDocx(DepartmentStaffPracticeDetailsViewModel practice)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            body.Append(CreateCenteredParagraph("МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ", true, 24));
            body.Append(CreateCenteredParagraph("федеральное государственное бюджетное образовательное учреждение высшего образования «Российский экономический университет имени Г.В. Плеханова»", false, 22));
            body.Append(CreateCenteredParagraph("______________________________________________________________________________________", false, 22));
            body.Append(CreateCenteredParagraph("Московский приборостроительный техникум", false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateCenteredParagraph("АТТЕСТАЦИОННЫЙ ЛИСТ", true, 28));
            body.Append(CreateCenteredParagraph("(характеристика профессиональной деятельности студента во время производственной практики)", false, 22));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("________________________________________________", false, 24));
            body.Append(CreateLeftParagraph("Фамилия, Имя, Отчество", false, 20));
            body.Append(CreateEmptyParagraph());

            var introText =
                $"обучающийся на _____ курсе в группе __________________ по специальности СПО {practice.SpecialtyCode} «{practice.SpecialtyName}» " +
                $"успешно прошел(ла) производственную практику ПП {practice.PracticeIndex} «{practice.Name}» " +
                $"по профессиональному модулю ПМ {practice.ProfessionalModuleCode} «{practice.ProfessionalModuleName}» " +
                $"в объеме {practice.Hours} часов в период: с {FormatDate(practice.StartDate)} по {FormatDate(practice.EndDate)}.";

            body.Append(CreateJustifiedParagraph(introText, false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Виды, объём и качество выполненных работ обучающимся во время практики", true, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateCompetenciesTable(practice));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Качество выполнения работ в соответствии с требованиями программы практики:", false, 24));
            body.Append(CreateLeftParagraph("_________________ (__________________)", false, 24));
            body.Append(CreateLeftParagraph("оценка цифрой           (оценка прописью)", false, 20));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("База прохождения производственной практики", true, 24));
            body.Append(CreateLeftParagraph("Предприятие (организация): _______________________________________________________", false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Руководитель практической подготовки от профильной организации", false, 24));
            body.Append(CreateLeftParagraph("________________    _________________    ______________", false, 24));
            body.Append(CreateLeftParagraph("Должность           ФИО                 Подпись", false, 20));
            body.Append(CreateLeftParagraph("Дата: «___» ______________ 2025 г.", false, 24));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Итоговая оценка по практике _________________________ (____________________)", false, 24));
            body.Append(CreateLeftParagraph("оценка цифрой                           (оценка прописью)", false, 20));
            body.Append(CreateEmptyParagraph());

            body.Append(CreateLeftParagraph("Руководитель практической подготовки от Московского приборостроительного техникума", false, 24));
            body.Append(CreateLeftParagraph("___________    ___________    ______________", false, 24));
            body.Append(CreateLeftParagraph("Должность      ФИО            Подпись", false, 20));
            body.Append(CreateLeftParagraph("Дата: «___» ______________ 2025 г.", false, 24));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public string BuildFileName(DepartmentStaffPracticeDetailsViewModel practice)
    {
        var safeIndex = string.Concat(practice.PracticeIndex.Where(ch =>
            char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_'));

        if (string.IsNullOrWhiteSpace(safeIndex))
            safeIndex = "practice";

        return $"Аттестационный_лист_{safeIndex}.docx";
    }

    private static Table CreateCompetenciesTable(DepartmentStaffPracticeDetailsViewModel practice)
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

        foreach (var competency in practice.Competencies)
        {
            var text = $"{BuildCompetencyTitle(competency)}\n{competency.WorkTypes}";
            table.Append(CreateTableRow(
                CreateTableCell(text, false, JustificationValues.Left),
                CreateTableCell(competency.Hours.ToString(), false, JustificationValues.Center)));
        }

        table.Append(CreateTableRow(
            CreateTableCell("Итого часов", true, JustificationValues.Left),
            CreateTableCell(practice.Hours.ToString(), true, JustificationValues.Center)));

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
        var cell = new TableCell();

        foreach (var line in text.Split('\n'))
        {
            var runProperties = new RunProperties(new FontSize { Val = "24" });
            if (bold)
                runProperties.Append(new Bold());

            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = justification }),
                new Run(runProperties, new Text(line) { Space = SpaceProcessingModeValues.Preserve }));

            cell.Append(paragraph);
        }

        cell.Append(new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));

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
        var runProperties = new RunProperties(new FontSize { Val = halfPoints.ToString() });
        if (bold)
            runProperties.Append(new Bold());

        return new Paragraph(
            new ParagraphProperties(new Justification { Val = justification }),
            new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreateEmptyParagraph()
    {
        return new Paragraph(new Run(new Text(" ")));
    }

    private static string BuildCompetencyTitle(DepartmentStaffPracticeCompetencyItemViewModel competency)
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
        return date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"));
    }
}