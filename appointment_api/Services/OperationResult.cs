namespace appointment_api.Services;

public class OperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? BookingId { get; init; }

    public static OperationResult CreateSuccess(string message, int? bookingId = null)
    {
        return new OperationResult { Success = true, Message = message, BookingId = bookingId };
    }

    public static OperationResult Fail(string message)
    {
        return new OperationResult { Success = false, Message = message };
    }
}
