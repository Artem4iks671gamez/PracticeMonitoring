namespace PracticeMonitoring.Api.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Course { get; set; } // целое число

    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public ICollection<User> Users { get; set; } = new List<User>();
}