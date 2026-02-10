namespace appointment_api.Models;

public class DayState
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int MaxSlots { get; set; }
    public int BookedCount { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
