using Xunit;
using appointment_api.Services;

namespace appointment_api.Tests;

public class OperationResultTests
{
    [Fact]
    public void CreateSuccess_ReturnsSuccessResult()
    {
        var result = OperationResult.CreateSuccess("Booked", 1);
        
        Assert.True(result.Success);
        Assert.Equal("Booked", result.Message);
        Assert.Equal(1, result.BookingId);
    }

    [Fact]
    public void Fail_ReturnsFailureResult()
    {
        var result = OperationResult.Fail("No slots available");
        
        Assert.False(result.Success);
        Assert.Equal("No slots available", result.Message);
    }
}