namespace PracticeMonitoring.Api.Dtos;

public class RegisterRequest
{
    public string Surname { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Patronymic { get; set; }
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? GroupId { get; set; } // выбранная группа (из списка)
}