namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeDiaryAttachment
{
    public int Id { get; set; }

    public int StudentPracticeDiaryEntryId { get; set; }

    public StudentPracticeDiaryEntry DiaryEntry { get; set; } = null!;

    public string Caption { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public byte[] Content { get; set; } = Array.Empty<byte>();

    public int SortOrder { get; set; }
}
