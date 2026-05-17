using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Tests.Services;

public class TemporaryPasswordServiceTests
{
    [Fact]
    public void Generate_ReturnsTwelveCharacterPasswordWithRequiredCharacterGroups()
    {
        var service = new TemporaryPasswordService();

        for (var i = 0; i < 25; i++)
        {
            var password = service.Generate();

            Assert.Equal(12, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
        }
    }
}
