using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using System.Security.Claims;

namespace StreetFood.Hubs;

[Authorize]
public class CurrentPickHub : Hub
{
    private readonly ICurrentPickRepository _currentPickRepository;

    public CurrentPickHub(ICurrentPickRepository currentPickRepository)
    {
        _currentPickRepository = currentPickRepository ?? throw new ArgumentNullException(nameof(currentPickRepository));
    }

    public async Task JoinRoom(int roomId)
    {
        var userId = GetCurrentUserId();

        var room = await _currentPickRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            throw new HubException("Khong tim thay phong Current Pick");
        }

        var isMember = await _currentPickRepository.IsMemberAsync(roomId, userId);
        if (!isMember)
        {
            throw new HubException("Ban chua tham gia phong nay");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(roomId));
    }

    public async Task JoinRoomByCode(string roomCode)
    {
        var userId = GetCurrentUserId();

        var normalizedCode = roomCode.Trim().ToUpperInvariant();
        var room = await _currentPickRepository.GetRoomByCodeAsync(normalizedCode);
        if (room == null)
        {
            throw new HubException("Khong tim thay phong Current Pick");
        }

        var isMember = await _currentPickRepository.IsMemberAsync(room.CurrentPickRoomId, userId);
        if (!isMember)
        {
            throw new HubException("Ban chua tham gia phong nay");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(room.CurrentPickRoomId));
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(roomId));
    }

    internal static string GetGroupName(int roomId) => $"current-pick:{roomId}";

    private int GetCurrentUserId()
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            throw new HubException("User not authenticated");
        }

        return userId;
    }
}
