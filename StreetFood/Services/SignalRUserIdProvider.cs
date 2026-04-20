using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace StreetFood.Services;

public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user?.FindFirst("userId")?.Value;
    }
}
