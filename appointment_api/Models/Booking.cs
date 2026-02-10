namespace appointment_api.Models;

public class Booking
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateOnly Date { get; set; }

    // can we user string for status  ? 
    // using an enum for status is better than a string because it provides type safety,
    // reduces the risk of typos, and makes the code more maintainable.
    
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CancelledByDoctor { get; set; }
}
