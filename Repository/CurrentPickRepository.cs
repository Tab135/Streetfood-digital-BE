using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class CurrentPickRepository : ICurrentPickRepository
{
    private readonly CurrentPickDAO _currentPickDao;

    public CurrentPickRepository(CurrentPickDAO currentPickDao)
    {
        _currentPickDao = currentPickDao ?? throw new ArgumentNullException(nameof(currentPickDao));
    }

    public Task<CurrentPickRoom> CreateRoomAsync(CurrentPickRoom room)
    {
        return _currentPickDao.CreateRoomAsync(room);
    }

    public Task AddMemberAsync(CurrentPickMember member)
    {
        return _currentPickDao.AddMemberAsync(member);
    }

    public Task<CurrentPickRoom?> GetRoomByIdAsync(int roomId, bool asNoTracking = true)
    {
        return _currentPickDao.GetRoomByIdAsync(roomId, asNoTracking);
    }

    public Task<CurrentPickMember?> GetMemberAsync(int roomId, int userId)
    {
        return _currentPickDao.GetMemberAsync(roomId, userId);
    }

    public Task<bool> IsMemberAsync(int roomId, int userId)
    {
        return _currentPickDao.IsMemberAsync(roomId, userId);
    }

    public Task<bool> ExistsRoomCodeAsync(string roomCode)
    {
        return _currentPickDao.ExistsRoomCodeAsync(roomCode);
    }

    public Task<Branch?> GetBranchForPickAsync(int branchId)
    {
        return _currentPickDao.GetBranchForPickAsync(branchId);
    }

    public Task<CurrentPickBranch?> GetRoomBranchAsync(int roomId, int branchId)
    {
        return _currentPickDao.GetRoomBranchAsync(roomId, branchId);
    }

    public Task AddRoomBranchAsync(CurrentPickBranch roomBranch)
    {
        return _currentPickDao.AddRoomBranchAsync(roomBranch);
    }

    public Task<CurrentPickVote?> GetVoteAsync(int roomId, int userId)
    {
        return _currentPickDao.GetVoteAsync(roomId, userId);
    }

    public Task AddVoteAsync(CurrentPickVote vote)
    {
        return _currentPickDao.AddVoteAsync(vote);
    }

    public Task UpdateVoteAsync(CurrentPickVote vote)
    {
        return _currentPickDao.UpdateVoteAsync(vote);
    }

    public Task<CurrentPickInvite?> GetInviteAsync(int roomId, int invitedUserId)
    {
        return _currentPickDao.GetInviteAsync(roomId, invitedUserId);
    }

    public Task AddInviteAsync(CurrentPickInvite invite)
    {
        return _currentPickDao.AddInviteAsync(invite);
    }

    public Task UpdateInviteAsync(CurrentPickInvite invite)
    {
        return _currentPickDao.UpdateInviteAsync(invite);
    }

    public Task UpdateRoomAsync(CurrentPickRoom room)
    {
        return _currentPickDao.UpdateRoomAsync(room);
    }
}
