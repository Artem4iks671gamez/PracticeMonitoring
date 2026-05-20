using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Tests.Services;

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_IncludesUserIdentityAndRoleClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "PracticeMonitoring.Api",
                ["Jwt:Audience"] = "PracticeMonitoring.Client",
                ["Jwt:ExpiresMinutes"] = "120"
            })
            .Build();

        var user = new User
        {
            Id = 42,
            Email = "student@example.com",
            FullName = "Иванов Иван Иванович",
            Surname = "Иванов",
            FirstName = "Иван",
            Role = new Role { Name = "Student" }
        };

        var token = new JwtService(configuration).GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        Assert.Equal("PracticeMonitoring.Api", jwt.Issuer);
        Assert.Equal("PracticeMonitoring.Client", jwt.Audiences.Single());
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Contains(jwt.Claims, x => x.Type == ClaimTypes.NameIdentifier && x.Value == "42");
        Assert.Contains(jwt.Claims, x => x.Type == ClaimTypes.Email && x.Value == "student@example.com");
        Assert.Contains(jwt.Claims, x => x.Type == ClaimTypes.Role && x.Value == "Student");
    }
}
