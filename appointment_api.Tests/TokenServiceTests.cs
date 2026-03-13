using Xunit;
using appointment_api.Services;
using Microsoft.Extensions.Configuration;

namespace appointment_api.Tests;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsNonNullToken()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"},
                {"Jwt:Key", "super_secret_key_12345678901234567890"}
            })
            .Build();
        
        var service = new TokenService(config);
        
        // Act
        var token = service.CreateToken(1, "patient");
        
        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }
}