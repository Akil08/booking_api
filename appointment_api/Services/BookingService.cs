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


        // does not executeudateasync itself solve the race condition ?
        // ExecuteUpdateAsync is used to atomically update the DayState record and 
        // check the conditions (not cancelled, slots available) in a single database operation.
        // no , i mean we use transaction to solve race condition, right b? 
        // Yes, the transaction ensures that the operations are atomic and isolated, preventing race conditions.
        // then exacuteupdateasync is just for performance optimization, right ?
        // Yes, ExecuteUpdateAsync allows us to perform the update in a single database round trip
        // and it also ensures that the update is done atomically, 
        // which is crucial for maintaining data integrity in a concurrent environment.
  
        // but what i am saying if if we just use executeupdateasync without transaction, it will still be atomic right ?
        // Yes, ExecuteUpdateAsync itself is atomic, but without a transaction, 
        // you won't be able to roll back if something goes wrong after the update.
        // For example, if the booking creation fails after the DayState is updated, 
        // you would end up with an inconsistent state where the DayState 
        // shows a slot as booked but there is no corresponding booking record.
        // Using a transaction allows you to ensure that either all operations succeed or none do, maintaining data integrity.

        // so then can we just use tracnaction and not use executeupdateasync ?
        // You could use a transaction without ExecuteUpdateAsync,
        //  but it would require you to first read the DayState record,
        //  check the conditions in your application code, and then update it.
        //  This would involve multiple database round trips and could lead to race conditions.
        // ExecuteUpdateAsync allows you to perform the check and update in a single database operation,
        //  which is more efficient and reduces the chances of race conditions.

        // reduce the chances of race conditions , what ???? i used transaction first to solve the race condition 
        // then u r saying wihtout executeudateasync we can not achive race conditon , how come ? 
        // The transaction helps to ensure that the operations are atomic and isolated, but it does not prevent race conditions
        // that can occur when multiple transactions are trying to update the same record at the same time.
        // ExecuteUpdateAsync allows you to perform the update in a single database operation, 
        // which can help to reduce the chances of race conditions by 
        // minimizing the window of time during which other transactions can interfere with the update.
        // Without ExecuteUpdateAsync, you would have to read the DayState record, 
        // check the conditions in your application code, and then update it, 
        // which involves multiple database round trips and increases the chances
        // of another transaction modifying the DayState record between your read and update operations, leading to a race condition.
        // so then if we use transaction and read the daystate record and then update it , it will be fine right ?
        // Yes, if you use a transaction and read the DayState record, check the conditions in your application code,
        //  and then update it, it can work. 
        // However, it is less efficient than using ExecuteUpdateAsync because it involves multiple database round trips 
        // (one for reading the DayState and another for updating it).

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
     
        // why not this savechanges is not enough to update the daystate record ?
        // This SaveChangesAsync call will update the booking record, 
        // but it won't automatically update the  DayState record to decrement the BookedCount.
        // You need to explicitly update the DayState record to reflect the cancellation of the booking.
        // aslo can we write this line before commiting the transaction ?
        // Yes, you can write the code to update the DayState record before committing the transaction.

        await _db.SaveChangesAsync();

        await _db.DayStates
            .Where(d => d.Date == today && d.BookedCount > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.BookedCount, d => d.BookedCount - 1)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

        await tx.CommitAsync();
   
        // this retrun type , who receive it ? 
        // The OperationResult returned by this method is typically received by the controller action that calls the CancelAsync method.

        // so what the frontend receives is the message and the success status, right ?
        // Yes, the frontend would receive the success status and the message contained in the OperationResult
        //  which can be used to inform the user about the outcome of their cancellation request. 


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
  
    // what is this method for ?
    // This method ensures that there is a DayState record for the current day. 
    // It is called at the beginning of booking and cancellation operations 
    // to make sure that the system has the necessary data to manage bookings for today. 
    // If there is no DayState for today, 
    // it creates one with default values (10 max slots, 0 booked, not cancelled). 
    // This helps prevent errors when trying to book or cancel appointments on a day
    //  that hasn't been initialized in the database.


    // does this fun is impoted from ohter file or its just a helper fun in this service ?
    // This function is a helper method within the BookingService class.
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
