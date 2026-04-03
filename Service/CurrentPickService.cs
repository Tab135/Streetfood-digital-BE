using BO.DTO.CurrentPick;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System.Globalization;

namespace Service;

public class CurrentPickService : ICurrentPickService
{
    private readonly ICurrentPickRepository _currentPickRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPickPusher _currentPickPusher;
    private readonly INotificationService _notificationService;

    private const int RoomCodeLength = 6;
    private const string RoomCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public CurrentPickService(
        ICurrentPickRepository currentPickRepository,
        IUserRepository userRepository,
        ICurrentPickPusher currentPickPusher,
        INotificationService notificationService)
    {
        _currentPickRepository = currentPickRepository ?? throw new ArgumentNullException(nameof(currentPickRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _currentPickPusher = currentPickPusher ?? throw new ArgumentNullException(nameof(currentPickPusher));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<CurrentPickRoomResponseDto> CreateRoomAsync(int hostUserId, CreateCurrentPickRoomDto dto)
    {
        await EnsureUserExistsAsync(hostUserId);

        var initialBranchIds = dto.InitialBranchIds.Distinct().ToList();
        foreach (var branchId in initialBranchIds)
        {
            var branch = await _currentPickRepository.GetBranchForPickAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Chi nhanh {branchId} khong hop le hoac khong hoat dong");
            }
        }

        var room = new CurrentPickRoom
        {
            HostUserId = hostUserId,
            RoomCode = await GenerateRoomCodeAsync(),
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsFinalized = false
        };

        room = await _currentPickRepository.CreateRoomAsync(room);

        await _currentPickRepository.AddMemberAsync(new CurrentPickMember
        {
            CurrentPickRoomId = room.CurrentPickRoomId,
            UserId = hostUserId,
            IsHost = true,
            JoinedAt = DateTime.UtcNow
        });

        foreach (var branchId in initialBranchIds)
        {
            await AddBranchInternalAsync(room.CurrentPickRoomId, hostUserId, branchId);
        }

        var latest = await RequireActiveRoomAsync(room.CurrentPickRoomId);
        return MapToRoomDto(latest, hostUserId);
    }

    public async Task<CurrentPickRoomResponseDto> GetRoomAsync(int roomId, int userId)
    {
        var room = await RequireMemberRoomAsync(roomId, userId);
        return MapToRoomDto(room, userId);
    }

    public async Task<CurrentPickInviteResponseDto> InviteUserAsync(int roomId, int hostUserId, InviteCurrentPickUserDto dto)
    {
        var room = await RequireMemberRoomAsync(roomId, hostUserId);
        EnsureHost(room, hostUserId);
        EnsureRoomNotFinalized(room);

        if (dto.UserId == hostUserId)
        {
            throw new DomainExceptions("Khong the tu moi chinh minh vao phong");
        }

        var invitedUser = await _userRepository.GetUserById(dto.UserId);
        if (invitedUser == null)
        {
            throw new DomainExceptions("Khong tim thay nguoi dung duoc moi");
        }

        if (room.Members.Any(m => m.UserId == invitedUser.Id))
        {
            throw new DomainExceptions("Nguoi dung nay da o trong phong");
        }

        var invite = await _currentPickRepository.GetInviteAsync(roomId, invitedUser.Id);
        if (invite == null)
        {
            invite = new CurrentPickInvite
            {
                CurrentPickRoomId = roomId,
                InvitedUserId = invitedUser.Id,
                InvitedByUserId = hostUserId,
                Status = CurrentPickInviteStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _currentPickRepository.AddInviteAsync(invite);
        }
        else
        {
            invite.Status = CurrentPickInviteStatus.Pending;
            invite.InvitedByUserId = hostUserId;
            invite.CreatedAt = DateTime.UtcNow;
            invite.RespondedAt = null;

            await _currentPickRepository.UpdateInviteAsync(invite);
        }

        var hostName = room.Members
            .Where(m => m.UserId == hostUserId)
            .Select(m => BuildDisplayName(m.User))
            .FirstOrDefault() ?? $"User {hostUserId}";

        await _notificationService.NotifyAsync(
            invitedUser.Id,
            NotificationType.CurrentPickInvite,
            "Loi moi Current Pick",
            $"{hostName} da moi ban tham gia phong Current Pick",
            room.CurrentPickRoomId,
            new
            {
                type = "current_pick_invite",
                roomId = room.CurrentPickRoomId,
                roomTitle = room.Title,
                invitedByUserId = hostUserId
            });

        return new CurrentPickInviteResponseDto
        {
            CurrentPickRoomId = room.CurrentPickRoomId,
            InvitedUserId = invitedUser.Id,
            InvitedByUserId = hostUserId,
            Status = invite.Status.ToString(),
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<CurrentPickRoomResponseDto> AcceptInviteAsync(int roomId, int userId)
    {
        await EnsureUserExistsAsync(userId);

        var room = await RequireActiveRoomAsync(roomId);
        var invite = await _currentPickRepository.GetInviteAsync(roomId, userId);

        if (invite == null)
        {
            throw new DomainExceptions("Ban khong co loi moi hop le vao phong nay", "ERR_FORBIDDEN");
        }

        if (invite.Status == CurrentPickInviteStatus.Accepted && room.Members.Any(m => m.UserId == userId))
        {
            return MapToRoomDto(room, userId);
        }

        if (room.IsFinalized)
        {
            throw new DomainExceptions("Phong da duoc chot quan", "ERR_FORBIDDEN");
        }

        if (invite.Status != CurrentPickInviteStatus.Pending)
        {
            throw new DomainExceptions("Loi moi nay khong con hieu luc", "ERR_FORBIDDEN");
        }

        var isMember = room.Members.Any(m => m.UserId == userId);
        if (!isMember)
        {
            await _currentPickRepository.AddMemberAsync(new CurrentPickMember
            {
                CurrentPickRoomId = room.CurrentPickRoomId,
                UserId = userId,
                IsHost = false,
                JoinedAt = DateTime.UtcNow
            });
        }

        invite.Status = CurrentPickInviteStatus.Accepted;
        invite.RespondedAt = DateTime.UtcNow;
        await _currentPickRepository.UpdateInviteAsync(invite);

        var snapshot = await RequireActiveRoomAsync(roomId);
        if (!isMember)
        {
            await BroadcastRoomUpdateAsync(snapshot, "member_joined");
        }

        return MapToRoomDto(snapshot, userId);
    }

    public async Task<CurrentPickBranchDto> AddBranchAsync(int roomId, int userId, AddCurrentPickBranchDto dto)
    {
        var room = await RequireMemberRoomAsync(roomId, userId);
        EnsureHost(room, userId);
        EnsureRoomNotFinalized(room);

        await AddBranchInternalAsync(roomId, userId, dto.BranchId);

        var snapshot = await RequireActiveRoomAsync(roomId);
        await BroadcastRoomUpdateAsync(snapshot, "branch_added");

        return MapBranches(snapshot).First(b => b.BranchId == dto.BranchId);
    }

    public async Task<CurrentPickRoomResponseDto> VoteAsync(int roomId, int userId, VoteCurrentPickDto dto)
    {
        var room = await RequireMemberRoomAsync(roomId, userId);
        EnsureRoomNotFinalized(room);

        var branchInRoom = room.Branches.Any(b => b.BranchId == dto.BranchId);
        if (!branchInRoom)
        {
            throw new DomainExceptions("Quan duoc chon khong ton tai trong phong");
        }

        var existingVote = await _currentPickRepository.GetVoteAsync(roomId, userId);
        if (existingVote == null)
        {
            await _currentPickRepository.AddVoteAsync(new CurrentPickVote
            {
                CurrentPickRoomId = roomId,
                BranchId = dto.BranchId,
                UserId = userId,
                VotedAt = DateTime.UtcNow
            });
        }
        else if (existingVote.BranchId != dto.BranchId)
        {
            existingVote.BranchId = dto.BranchId;
            await _currentPickRepository.UpdateVoteAsync(existingVote);
        }

        var snapshot = await RequireActiveRoomAsync(roomId);
        await BroadcastRoomUpdateAsync(snapshot, "vote_updated");

        return MapToRoomDto(snapshot, userId);
    }

    public async Task<FinalizedCurrentPickDto> FinalizeAsync(int roomId, int userId, FinalizeCurrentPickDto dto)
    {
        var room = await RequireMemberRoomAsync(roomId, userId);
        EnsureHost(room, userId);
        EnsureRoomNotFinalized(room);

        if (room.Branches.Count == 0)
        {
            throw new DomainExceptions("Phong chua co quan de chot");
        }

        var finalizedBranchId = dto.BranchId ?? ChooseWinningBranchId(room);
        var finalizedBranch = room.Branches.FirstOrDefault(b => b.BranchId == finalizedBranchId)?.Branch;

        if (finalizedBranch == null)
        {
            throw new DomainExceptions("Quan duoc chot khong hop le");
        }

        var trackedRoom = await _currentPickRepository.GetRoomByIdAsync(roomId, asNoTracking: false)
            ?? throw new DomainExceptions("Khong tim thay phong Current Pick");

        trackedRoom.IsFinalized = true;
        trackedRoom.FinalizedAt = DateTime.UtcNow;
        trackedRoom.FinalizedBranchId = finalizedBranchId;

        await _currentPickRepository.UpdateRoomAsync(trackedRoom);

        var snapshot = await RequireActiveRoomAsync(roomId);
        await BroadcastRoomUpdateAsync(snapshot, "room_finalized");

        var lat = finalizedBranch.Lat.ToString(CultureInfo.InvariantCulture);
        var lng = finalizedBranch.Long.ToString(CultureInfo.InvariantCulture);

        return new FinalizedCurrentPickDto
        {
            CurrentPickRoomId = roomId,
            FinalizedBranchId = finalizedBranchId,
            FinalizedBranchName = finalizedBranch.Name,
            Lat = finalizedBranch.Lat,
            Long = finalizedBranch.Long,
            MapUrl = $"https://www.google.com/maps/search/?api=1&query={lat},{lng}",
            Room = MapToRoomDto(snapshot, userId)
        };
    }

    public async Task ClearRoomAsync(int roomId, int userId)
    {
        var room = await RequireMemberRoomAsync(roomId, userId);
        EnsureHost(room, userId);

        var trackedRoom = await _currentPickRepository.GetRoomByIdAsync(roomId, asNoTracking: false)
            ?? throw new DomainExceptions("Khong tim thay phong Current Pick");

        trackedRoom.IsActive = false;
        trackedRoom.UpdatedAt = DateTime.UtcNow;

        await _currentPickRepository.UpdateRoomAsync(trackedRoom);

        var payload = new CurrentPickRealtimeEventDto
        {
            EventType = "room_cleared",
            Room = MapToRoomDto(trackedRoom, null)
        };
        payload.Room.IsActive = false;

        await _currentPickPusher.PushRoomUpdatedAsync(roomId, payload);
    }

    private async Task AddBranchInternalAsync(int roomId, int addedByUserId, int branchId)
    {
        var branch = await _currentPickRepository.GetBranchForPickAsync(branchId)
            ?? throw new DomainExceptions("Khong tim thay quan hop le");

        var existing = await _currentPickRepository.GetRoomBranchAsync(roomId, branch.BranchId);
        if (existing != null)
        {
            throw new DomainExceptions("Quan nay da co trong phong");
        }

        await _currentPickRepository.AddRoomBranchAsync(new CurrentPickBranch
        {
            CurrentPickRoomId = roomId,
            BranchId = branch.BranchId,
            AddedByUserId = addedByUserId,
            AddedAt = DateTime.UtcNow
        });
    }

    private static int ChooseWinningBranchId(CurrentPickRoom room)
    {
        var voteCounts = room.Votes
            .GroupBy(v => v.BranchId)
            .ToDictionary(g => g.Key, g => g.Count());

        return room.Branches
            .OrderByDescending(b => voteCounts.GetValueOrDefault(b.BranchId, 0))
            .ThenBy(b => b.AddedAt)
            .ThenBy(b => b.BranchId)
            .Select(b => b.BranchId)
            .First();
    }

    private async Task<CurrentPickRoom> RequireActiveRoomAsync(int roomId)
    {
        return await _currentPickRepository.GetRoomByIdAsync(roomId)
            ?? throw new DomainExceptions("Khong tim thay phong Current Pick");
    }

    private async Task<CurrentPickRoom> RequireMemberRoomAsync(int roomId, int userId)
    {
        var room = await RequireActiveRoomAsync(roomId);
        var isMember = room.Members.Any(m => m.UserId == userId);
        if (!isMember)
        {
            throw new DomainExceptions("Ban khong thuoc phong Current Pick nay", "ERR_FORBIDDEN");
        }

        return room;
    }

    private static void EnsureHost(CurrentPickRoom room, int userId)
    {
        if (room.HostUserId != userId)
        {
            throw new DomainExceptions("Chi host moi co quyen thuc hien thao tac nay", "ERR_FORBIDDEN");
        }
    }

    private static void EnsureRoomNotFinalized(CurrentPickRoom room)
    {
        if (room.IsFinalized)
        {
            throw new DomainExceptions("Phong da duoc chot quan");
        }
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            throw new DomainExceptions("Khong tim thay nguoi dung");
        }
    }

    private async Task<string> GenerateRoomCodeAsync()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var chars = Enumerable.Range(0, RoomCodeLength)
                .Select(_ => RoomCodeChars[Random.Shared.Next(RoomCodeChars.Length)])
                .ToArray();

            var code = new string(chars);
            if (!await _currentPickRepository.ExistsRoomCodeAsync(code))
            {
                return code;
            }
        }

        throw new DomainExceptions("Khong the tao ma phong. Vui long thu lai");
    }

    private async Task BroadcastRoomUpdateAsync(CurrentPickRoom room, string eventType)
    {
        var payload = new CurrentPickRealtimeEventDto
        {
            EventType = eventType,
            Room = MapToRoomDto(room, null)
        };

        await _currentPickPusher.PushRoomUpdatedAsync(room.CurrentPickRoomId, payload);
    }

    private CurrentPickRoomResponseDto MapToRoomDto(CurrentPickRoom room, int? requesterUserId)
    {
        return new CurrentPickRoomResponseDto
        {
            CurrentPickRoomId = room.CurrentPickRoomId,
            RoomCode = room.RoomCode,
            Title = room.Title,
            HostUserId = room.HostUserId,
            IsActive = room.IsActive,
            IsFinalized = room.IsFinalized,
            FinalizedBranchId = room.FinalizedBranchId,
            CreatedAt = room.CreatedAt,
            FinalizedAt = room.FinalizedAt,
            MyVotedBranchId = requesterUserId.HasValue
                ? room.Votes.FirstOrDefault(v => v.UserId == requesterUserId.Value)?.BranchId
                : null,
            Members = room.Members
                .OrderBy(m => m.JoinedAt)
                .Select(m => new CurrentPickMemberDto
                {
                    UserId = m.UserId,
                    DisplayName = BuildDisplayName(m.User),
                    AvatarUrl = m.User.AvatarUrl,
                    IsHost = m.IsHost,
                    JoinedAt = m.JoinedAt
                })
                .ToList(),
            Branches = MapBranches(room)
        };
    }

    private static List<CurrentPickBranchDto> MapBranches(CurrentPickRoom room)
    {
        var voteCounts = room.Votes
            .GroupBy(v => v.BranchId)
            .ToDictionary(g => g.Key, g => g.Count());

        return room.Branches
            .OrderBy(b => b.AddedAt)
            .Select(b => new CurrentPickBranchDto
            {
                BranchId = b.BranchId,
                Name = b.Branch.Name,
                AddressDetail = b.Branch.AddressDetail,
                City = b.Branch.City,
                Lat = b.Branch.Lat,
                Long = b.Branch.Long,
                ImageUrl = b.Branch.BranchImages.FirstOrDefault()?.ImageUrl,
                AddedByUserId = b.AddedByUserId,
                AddedAt = b.AddedAt,
                VoteCount = voteCounts.GetValueOrDefault(b.BranchId, 0)
            })
            .ToList();
    }

    private static string BuildDisplayName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            return user.Email;
        }

        return $"User {user.Id}";
    }

}
