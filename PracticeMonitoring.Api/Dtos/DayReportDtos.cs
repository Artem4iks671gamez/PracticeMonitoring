using System.Text.Json.Serialization;

namespace PracticeMonitoring.Api.Dtos.Student;

public class DayReportDto
{
    public int Version { get; set; }

    public string Type { get; set; } = "practice-day-report";

    public List<ReportBlockDto> Blocks { get; set; } = new();
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextReportBlockDto), "text")]
[JsonDerivedType(typeof(TableReportBlockDto), "table")]
[JsonDerivedType(typeof(ImageReportBlockDto), "image")]
public abstract class ReportBlockDto
{
    public string? Id { get; set; }
}

public class TextReportBlockDto : ReportBlockDto
{
    public string? Content { get; set; }

    public string? Mode { get; set; }
}

public class TableReportBlockDto : ReportBlockDto
{
    public string? Title { get; set; }

    public bool HasHeaderRow { get; set; }

    public List<TableRowDto> Rows { get; set; } = new();
}

public class ImageReportBlockDto : ReportBlockDto
{
    public string? Title { get; set; }

    public int? AttachmentId { get; set; }

    public string? UploadClientId { get; set; }

    public string? ImageUrl { get; set; }

    public string? Alt { get; set; }
}

public class TableRowDto
{
    public string? Id { get; set; }

    public List<TableCellDto> Cells { get; set; } = new();
}

public class TableCellDto
{
    public string? Id { get; set; }

    public string? Text { get; set; }

    public int Colspan { get; set; } = 1;

    public int Rowspan { get; set; } = 1;

    public bool Hidden { get; set; }
}
