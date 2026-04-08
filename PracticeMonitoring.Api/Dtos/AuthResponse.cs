namespace PracticeMonitoring.Api.Dtos;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
}