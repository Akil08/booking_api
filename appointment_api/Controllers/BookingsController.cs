using appointment_api.DTOs;
using appointment_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace appointment_api.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Roles = "patient")]
    [HttpPost("book")]
    public async Task<ActionResult<BookingResponse>> Book()
    {
        var patientId = GetUserId();
        if (patientId == null)
        {
            return Unauthorized();
        }

        var result = await _bookingService.BookAsync(patientId.Value);
        
         if ( !result.Success )
        {
            return BadRequest(new MessageResponse(result.Message));
        }

        return Ok(new BookingResponse(result.BookingId ?? 0, result.Message));
    }

    [Authorize(Roles = "patient")]
    [HttpPost("cancel")]
    public async Task<ActionResult<MessageResponse>> Cancel()
    {   
        // i know what it does but how it gets is ? 
        // it gets the user id from the JWT token claims, which is set during authentication.
        var patientId = GetUserId();
        if (patientId == null)
        {
            return Unauthorized();
        }

        var result = await _bookingService.CancelAsync(patientId.Value);
        if (!result.Success)
        {
            return BadRequest(new MessageResponse(result.Message));
        }

        return Ok(new MessageResponse(result.Message));
    }

    [Authorize(Roles = "doctor")]
    [HttpPost("doctor/cancel-day")]
    public async Task<ActionResult<MessageResponse>> DoctorCancelDay()
    {
        var result = await _bookingService.DoctorCancelDayAsync();
        if (!result.Success)
        {
            return BadRequest(new MessageResponse(result.Message));
        }

        return Ok(new MessageResponse(result.Message));
    }

    [Authorize(Roles = "patient")]
    [HttpPost("priority/subscribe")]
    public async Task<ActionResult<MessageResponse>> SubscribePriority()
    {
        var patientId = GetUserId();
        if (patientId == null)
        {
            return Unauthorized();
        }

        var result = await _bookingService.SubscribePriorityAsync(patientId.Value);
        if (!result.Success)
        {
            return BadRequest(new MessageResponse(result.Message));
        }

        return Ok(new MessageResponse(result.Message));
    }
       private int? GetUserId()
    {   
        var claim = User.FindFirst("id")?.Value;
        if (int.TryParse(claim, out var id))
        {
            return id;
        }

        return null;
    }
}
