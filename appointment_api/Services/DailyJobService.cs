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
 
    // who uses this fun ? 
    // This function is intended to be called by a scheduled job (like a cron job) that runs once a day,
    // typically at midnight. It resets the daily state of the system by clearing out old bookings
    // and preparing for new bookings for the day. It ensures that the system starts each day with a clean slate,
    // allowing patients to book appointments for the new day and ensuring that any unfulfilled bookings from
    // the previous day are cancelled and handled appropriately. 

    //  i mean how does it get called ?
    //  i used hangfire , so what i can see is from program.cs only rundailyreset async it called 
    // not ensuretoday async so how does it get called ?
    // booking service has its own ensuretoday async method which is called at the beginning of booking and 
    // cancellation operations to make sure that there is a DayState for today.
    // so why we need ensuretoday async in this service if we have it in booking service ?
    // The EnsureTodayAsync method in the DailyJobService is specifically designed to be called by
    // the scheduled job that runs daily to reset the state of the system.
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
        // do this fun called ensure today async ?
        // No, the RunDailyResetAsync method does not call EnsureTodayAsync.
        // then why ensuretoday async is in this service if it is not called by any fun in this service ?
        // The EnsureTodayAsync method is included in the DailyJobService to provide a way to
        // ensure that there is a DayState for the current day, which is necessary for the booking system to function correctly.
        // bookigb serviec has its own ensuretoday async, i think we can 
        // remove it from here . rihgt ? 
        // We can remove the EnsureTodayAsync method from the DailyJobService if it is not being called by any other method in that service.
        // However, if there is a possibility that it might be needed in the future for other

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
