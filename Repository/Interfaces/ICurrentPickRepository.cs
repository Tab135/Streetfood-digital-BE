using BO.Entities;

namespace Repository.Interfaces;

public interface ICurrentPickRepository
{
    Task<CurrentPickRoom> CreateRoomAsync(CurrentPickRoom room);
    Task AddMemberAsync(CurrentPickMember member);
    Task<CurrentPickRoom?> GetRoomByIdAsync(int roomId, bool asNoTracking = true);
    Task<CurrentPickMember?> GetMemberAsync(int roomId, int userId);
    Task<bool> IsMemberAsync(int roomId, int userId);
    Task<bool> ExistsRoomCodeAsync(string roomCode);
    Task<Branch?> GetBranchForPickAsync(int branchId);
    Task<CurrentPickBranch?> GetRoomBranchAsync(int roomId, int branchId);
    Task AddRoomBranchAsync(CurrentPickBranch roomBranch);
    Task<CurrentPickVote?> GetVoteAsync(int roomId, int userId);
    Task AddVoteAsync(CurrentPickVote vote);
    Task UpdateVoteAsync(CurrentPickVote vote);
    Task<CurrentPickInvite?> GetInviteAsync(int roomId, int invitedUserId);
    Task AddInviteAsync(CurrentPickInvite invite);
    Task UpdateInviteAsync(CurrentPickInvite invite);
    Task UpdateRoomAsync(CurrentPickRoom room);
}
