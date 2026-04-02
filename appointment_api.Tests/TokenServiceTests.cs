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

        // is this test a good unit test ?
        // This test is a good unit test for the CreateToken method of the TokenService class.
        // It verifies that the method returns a non-null and non-empty token 
        // when provided with valid input parameters.
        // The test uses an in-memory configuration to set up the necessary JWT settings,
        // which allows it to run independently of any external configuration files or services.
        

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