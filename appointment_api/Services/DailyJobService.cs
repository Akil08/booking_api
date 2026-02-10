using appointment_api.Data;
using appointment_api.Models;
using Microsoft.EntityFrameworkCore;

namespace appointment_api.Services;

public class DailyJobService
{
    private readonly AppDbContext _db;

    public DailyJobService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureTodayAsync()
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

    public async Task RunDailyResetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _db.Database.BeginTransactionAsync();

        var day = await _db.DayStates.SingleOrDefaultAsync(d => d.Date == today);
        if (day == null)
        {
            day = new DayState
            {
                Date = today,
                MaxSlots = 10,
                BookedCount = 0,
                IsCancelled = false,
                UpdatedAt = DateTime.UtcNow
            };
            _db.DayStates.Add(day);
            await _db.SaveChangesAsync();
        }
        else
        {
            day.MaxSlots = 10;
            day.BookedCount = 0;
            day.IsCancelled = false;
            day.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await _db.Bookings
            .Where(b => b.Date == today && b.Status == BookingStatus.Booked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Cancelled)
                .SetProperty(b => b.CancelledAt, DateTime.UtcNow)
                .SetProperty(b => b.CancelledByDoctor, false));

        var queue = await _db.PrioritySubscribers
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        foreach (var sub in queue)
        {
            var alreadyBooked = await _db.Bookings.AnyAsync(b => b.PatientId == sub.PatientId && b.Date == today && b.Status == BookingStatus.Booked);
            if (alreadyBooked)
            {
                continue;
            }

            var updated = await _db.DayStates
                .Where(d => d.Date == today && !d.IsCancelled && d.BookedCount < d.MaxSlots)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.BookedCount, d => d.BookedCount + 1)
                    .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

            if (updated == 0)
            {
                break;
            }

            _db.Bookings.Add(new Booking
            {
                PatientId = sub.PatientId,
                Date = today,
                Status = BookingStatus.Booked,
                CreatedAt = DateTime.UtcNow,
                CancelledByDoctor = false
            });
        }

        _db.PrioritySubscribers.RemoveRange(queue);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
