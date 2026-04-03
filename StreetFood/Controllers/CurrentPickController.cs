using BO.DTO.CurrentPick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace StreetFood.Controllers;

[ApiController]
[Route("api/current-pick")]
[Authorize]
public class CurrentPickController : ControllerBase
{
    private readonly ICurrentPickService _currentPickService;

    public CurrentPickController(ICurrentPickService currentPickService)
    {
        _currentPickService = currentPickService ?? throw new ArgumentNullException(nameof(currentPickService));
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateCurrentPickRoomDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var room = await _currentPickService.CreateRoomAsync(userId, dto ?? new CreateCurrentPickRoomDto());
        return Ok(new
        {
            message = "Current Pick room created successfully",
            data = room
        });
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinRoom([FromBody] JoinCurrentPickRoomDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var room = await _currentPickService.JoinRoomAsync(userId, dto);
        return Ok(new
        {
            message = "Joined Current Pick room successfully",
            data = room
        });
    }

    [HttpGet("rooms/{roomId:int}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var room = await _currentPickService.GetRoomAsync(roomId, userId);
        return Ok(new
        {
            message = "Current Pick room fetched successfully",
            data = room
        });
    }

    [HttpGet("rooms/{roomId:int}/share-link")]
    public async Task<IActionResult> GetShareLink(int roomId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _currentPickService.GetShareLinkAsync(roomId, userId);
        return Ok(new
        {
            message = "Current Pick share link generated",
            data = result
        });
    }

    [HttpPost("rooms/{roomId:int}/branches")]
    public async Task<IActionResult> AddBranch(int roomId, [FromBody] AddCurrentPickBranchDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var branch = await _currentPickService.AddBranchAsync(roomId, userId, dto);
        return Ok(new
        {
            message = "Branch added to Current Pick room successfully",
            data = branch
        });
    }

    [HttpPost("rooms/{roomId:int}/vote")]
    public async Task<IActionResult> Vote(int roomId, [FromBody] VoteCurrentPickDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _currentPickService.VoteAsync(roomId, userId, dto);
        return Ok(new
        {
            message = "Vote submitted successfully",
            data = result
        });
    }

    [HttpPost("rooms/{roomId:int}/finalize")]
    public async Task<IActionResult> FinalizeRoom(int roomId, [FromBody] FinalizeCurrentPickDto? dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _currentPickService.FinalizeAsync(roomId, userId, dto ?? new FinalizeCurrentPickDto());
        return Ok(new
        {
            message = "Current Pick room finalized successfully",
            data = result
        });
    }

    [HttpDelete("rooms/{roomId:int}")]
    public async Task<IActionResult> ClearRoom(int roomId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        await _currentPickService.ClearRoomAsync(roomId, userId);
        return Ok(new
        {
            message = "Current Pick room cleared successfully"
        });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
    }
}
