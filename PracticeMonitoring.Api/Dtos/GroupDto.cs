namespace PracticeMonitoring.Api.Dtos;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Course { get; set; }
    public int SpecialtyId { get; set; }
}