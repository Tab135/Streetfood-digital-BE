using BO.DTO.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Interfaces;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace StreetFood.Controllers;

[Route("api/user/pin")]
[ApiController]
[Authorize]
public class UserPinController : ControllerBase
{
    private static readonly Regex PinRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    private readonly IUserPinService _pinService;

    public UserPinController(IUserPinService pinService)
    {
        _pinService = pinService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        var result = await _pinService.GetStatusAsync(userId);
        return Ok(new { message = "PIN status retrieved", data = result });
    }

    [HttpPost("set")]
    public async Task<IActionResult> SetPin([FromBody] SetPinDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        if (!PinRegex.IsMatch(dto.Pin))
            return BadRequest(new { message = "PIN must be exactly 6 digits." });

        try
        {
            await _pinService.SetPinAsync(userId, dto.Pin);
            return Ok(new { message = "PIN set successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPin([FromBody] VerifyPinDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        if (!PinRegex.IsMatch(dto.Pin))
            return BadRequest(new { message = "PIN must be exactly 6 digits." });

        try
        {
            var result = await _pinService.VerifyPinAsync(userId, dto.Pin);
            return Ok(new { message = "Verification complete", data = result });
        }
        catch (PinLockedException ex)
        {
            return StatusCode(429, new { message = ex.Message, retryAfter = ex.RetryAfterSeconds });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change")]
    public async Task<IActionResult> ChangePin([FromBody] ChangePinDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        if (!PinRegex.IsMatch(dto.CurrentPin) || !PinRegex.IsMatch(dto.NewPin))
            return BadRequest(new { message = "PIN must be exactly 6 digits." });

        try
        {
            await _pinService.ChangePinAsync(userId, dto.CurrentPin, dto.NewPin);
            return Ok(new { message = "PIN changed successfully" });
        }
        catch (PinLockedException ex)
        {
            return StatusCode(429, new { message = ex.Message, retryAfter = ex.RetryAfterSeconds });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemovePin([FromBody] RemovePinDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        if (!PinRegex.IsMatch(dto.Pin))
            return BadRequest(new { message = "PIN must be exactly 6 digits." });

        try
        {
            await _pinService.RemovePinAsync(userId, dto.Pin);
            return Ok(new { message = "PIN removed successfully" });
        }
        catch (PinLockedException ex)
        {
            return StatusCode(429, new { message = ex.Message, retryAfter = ex.RetryAfterSeconds });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst("userId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }
}
