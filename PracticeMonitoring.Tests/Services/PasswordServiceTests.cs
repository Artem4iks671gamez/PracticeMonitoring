using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Tests.Services;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_CreatesHashThatVerifiesOriginalPassword()
    {
        var service = new PasswordService();
        var user = new User
        {
            Email = "student@example.com",
            FullName = "Иванов Иван Иванович",
            Surname = "Иванов",
            FirstName = "Иван",
            Role = new Role { Name = "Student" }
        };

        user.PasswordHash = service.HashPassword(user, "StrongPass123!");

        Assert.NotEqual("StrongPass123!", user.PasswordHash);
        Assert.True(service.VerifyPassword(user, "StrongPass123!"));
        Assert.False(service.VerifyPassword(user, "WrongPass123!"));
    }
}
