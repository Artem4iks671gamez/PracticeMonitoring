namespace PracticeMonitoring.Web.Models.Messaging;

public class ChatFileResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "attachment.bin";
}
