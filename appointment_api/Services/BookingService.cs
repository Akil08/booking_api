using appointment_api.Data;
using appointment_api.Models;
using Microsoft.EntityFrameworkCore;

namespace appointment_api.Services;

public class BookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult> BookAsync(int patientId)
    {
        await EnsureTodayAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _db.Database.BeginTransactionAsync();

        var alreadyBooked = await _db.Bookings.AnyAsync(b => b.PatientId == patientId && b.Date == today && b.Status == BookingStatus.Booked);
        if (alreadyBooked)
        {
            await tx.RollbackAsync();
            return OperationResult.Fail("Already booked for today");
        }

        var updated = await _db.DayStates
            .Where(d => d.Date == today && !d.IsCancelled && d.BookedCount < d.MaxSlots)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.BookedCount, d => d.BookedCount + 1)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));


        if (updated == 0)
        {
            await tx.RollbackAsync();
            return OperationResult.Fail("No slots available");
        }

        var booking = new Booking
        {
            PatientId = patientId,
            Date = today,
            Status = BookingStatus.Booked,
            CreatedAt = DateTime.UtcNow,
            CancelledByDoctor = false
        };

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return OperationResult.CreateSuccess("Booked", booking.Id);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return OperationResult.Fail("Booking failed");
        }
    }

    public async Task<OperationResult> CancelAsync(int patientId)
    {
        await EnsureTodayAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _db.Database.BeginTransactionAsync();

        var booking = await _db.Bookings.SingleOrDefaultAsync(b => b.PatientId == patientId && b.Date == today && b.Status == BookingStatus.Booked);
        if (booking == null)
        {
            await tx.RollbackAsync();
            return OperationResult.Fail("No active booking found");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledByDoctor = false;
        await _db.SaveChangesAsync();

        await _db.DayStates
            .Where(d => d.Date == today && d.BookedCount > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.BookedCount, d => d.BookedCount - 1)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

        await tx.CommitAsync();

        return OperationResult.CreateSuccess("Booking cancelled");
    }

    public async Task<OperationResult> SubscribePriorityAsync(int patientId)
    {
        await EnsureTodayAsync();

        var exists = await _db.PrioritySubscribers.AnyAsync(p => p.PatientId == patientId);
        if (exists)
        {
            return OperationResult.CreateSuccess("Already subscribed");
        }

        _db.PrioritySubscribers.Add(new PrioritySubscriber
        {
            PatientId = patientId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return OperationResult.CreateSuccess("Subscribed for priority booking");
    }

    public async Task<OperationResult> DoctorCancelDayAsync()
    {
        await EnsureTodayAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _db.Database.BeginTransactionAsync();

        var day = await _db.DayStates.SingleAsync(d => d.Date == today);
        if (day.IsCancelled)
        {
            await tx.RollbackAsync();
            return OperationResult.Fail("Day already cancelled");
        }

        var patients = await _db.Bookings
            .Where(b => b.Date == today && b.Status == BookingStatus.Booked)
            .Select(b => b.PatientId)
            .ToListAsync();

        await _db.Bookings
            .Where(b => b.Date == today && b.Status == BookingStatus.Booked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Cancelled)
                .SetProperty(b => b.CancelledAt, DateTime.UtcNow)
                .SetProperty(b => b.CancelledByDoctor, true));

        day.IsCancelled = true;
        day.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        foreach (var id in patients)
        {
            Console.WriteLine($"Email to patient {id}: Your appointment was cancelled by the doctor. You can subscribe for priority booking for tomorrow.");
        }

        return OperationResult.CreateSuccess("Day cancelled and patients notified");
    }
    private async Task EnsureTodayAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var exists = await _db.DayStates.AnyAsync(d => d.Date == today);
        if (!exists)
        {
            _db.DayStates.Add(new DayState
            {
                Date = today,
                MaxSlots = 10,
                BookedCount = 0,
                IsCancelled = false,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
