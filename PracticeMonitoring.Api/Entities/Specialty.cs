namespace PracticeMonitoring.Api.Entities;

public class Specialty
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<Group> Groups { get; set; } = new List<Group>();
}