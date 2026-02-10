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
   

    // Helper method to extract user ID from JWT claims
    // but we can also create a user response dto and collect user id from there , rigiht ? 
    // Yes, you can create a UserResponse DTO that includes the user ID and other relevant information. 
    // This DTO can be returned by an endpoint that provides user details after authentication. 
    // However, for actions that require the user ID, 
    // it's common to extract it directly from the JWT claims as shown in the GetUserId() method. 
    // This way, you don't need to make an additional request to get the user ID for each action, 
    // and it keeps the flow more efficient.
    // no no, suppose we r taking some response from user but there was no filed in the response dto from user
    // then how we will get the user id ?
    // In that case, you would need to ensure that the user ID is included in the response DTO 
    // when the user logs in or when their details are fetched. 
    // If the user ID is not included in the response DTO, you would not be able to get it directly from the response. 
    // However, if the user is authenticated and you are using JWT tokens, the user ID should be included in the token's claims. 
    // When the user logs in, you can include the user ID in the JWT token.

    // that means wheheven we use jwt token we have to include user id in the claims right ?
    // Yes, it's a common practice to include the user ID in the claims of a JWT token. 
    // This allows you to easily identify the user making the request and perform actions based on their identity.
    private int? GetUserId()
    {   
        // waht is user ? 
        // so its from jwt ? 
        // Yes, the User property in ASP.NET Core is part of the HttpContext and represents the currently authenticated user. 
        // When a user is authenticated using JWT tokens, the claims from the token are available in the User property. 
        // You can access the claims to get information about the user, such as their ID, roles, etc. In this case, we are trying to get the "id" claim from the JWT token to identify the user.    
        // but findfirst is it a method of user ?
        // Yes, FindFirst is a method of the ClaimsPrincipal class, which is the type of the User property. 
        // It is used to find the first claim that matches a specified type. In this case, we are looking for the claim with the type "id" to get the user's ID from the JWT token.
        // but its fell like we r looping throuth all users ? 
        // No, FindFirst does not loop through all users. It only searches 
        // through the claims of the currently authenticated user, which is represented by the User property. 
        // So it is efficient and only looks at the claims of the current user.
        var claim = User.FindFirst("id")?.Value;
        if (int.TryParse(claim, out var id))
        {
            return id;
        }

        return null;
    }
}
