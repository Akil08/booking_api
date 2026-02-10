namespace appointment_api.Models;

public class PrioritySubscriber
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime CreatedAt { get; set; }
}
