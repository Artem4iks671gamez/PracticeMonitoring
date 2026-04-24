namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffFileResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "download.bin";
}
