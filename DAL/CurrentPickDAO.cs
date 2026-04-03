using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class CurrentPickDAO
{
    private readonly StreetFoodDbContext _context;

    public CurrentPickDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<CurrentPickRoom> BuildRoomQuery(bool asNoTracking = true)
    {
        IQueryable<CurrentPickRoom> query = _context.CurrentPickRooms
            .AsSplitQuery()
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .Include(r => r.Branches)
                .ThenInclude(rb => rb.Branch)
                    .ThenInclude(b => b.BranchImages)
            .Include(r => r.Votes)
            .Include(r => r.FinalizedBranch)
                .ThenInclude(b => b!.BranchImages);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    public async Task<CurrentPickRoom> CreateRoomAsync(CurrentPickRoom room)
    {
        _context.CurrentPickRooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task AddMemberAsync(CurrentPickMember member)
    {
        _context.CurrentPickMembers.Add(member);
        await _context.SaveChangesAsync();
    }

    public async Task<CurrentPickRoom?> GetRoomByIdAsync(int roomId, bool asNoTracking = true)
    {
        return await BuildRoomQuery(asNoTracking)
            .FirstOrDefaultAsync(r => r.CurrentPickRoomId == roomId && r.IsActive);
    }

    public async Task<CurrentPickRoom?> GetRoomByCodeAsync(string roomCode, bool asNoTracking = true)
    {
        return await BuildRoomQuery(asNoTracking)
            .FirstOrDefaultAsync(r => r.RoomCode == roomCode && r.IsActive);
    }

    public async Task<CurrentPickMember?> GetMemberAsync(int roomId, int userId)
    {
        return await _context.CurrentPickMembers
            .FirstOrDefaultAsync(m => m.CurrentPickRoomId == roomId && m.UserId == userId);
    }

    public async Task<bool> IsMemberAsync(int roomId, int userId)
    {
        return await _context.CurrentPickMembers
            .AnyAsync(m => m.CurrentPickRoomId == roomId && m.UserId == userId);
    }

    public async Task<bool> ExistsRoomCodeAsync(string roomCode)
    {
        return await _context.CurrentPickRooms.AnyAsync(r => r.RoomCode == roomCode);
    }

    public async Task<Branch?> GetBranchForPickAsync(int branchId)
    {
        return await _context.Branches
            .AsNoTracking()
            .Include(b => b.BranchImages)
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.IsActive && b.IsVerified);
    }

    public async Task<CurrentPickBranch?> GetRoomBranchAsync(int roomId, int branchId)
    {
        return await _context.CurrentPickBranches
            .FirstOrDefaultAsync(rb => rb.CurrentPickRoomId == roomId && rb.BranchId == branchId);
    }

    public async Task AddRoomBranchAsync(CurrentPickBranch roomBranch)
    {
        _context.CurrentPickBranches.Add(roomBranch);
        await _context.SaveChangesAsync();
    }

    public async Task<CurrentPickVote?> GetVoteAsync(int roomId, int userId)
    {
        return await _context.CurrentPickVotes
            .FirstOrDefaultAsync(v => v.CurrentPickRoomId == roomId && v.UserId == userId);
    }

    public async Task AddVoteAsync(CurrentPickVote vote)
    {
        _context.CurrentPickVotes.Add(vote);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateVoteAsync(CurrentPickVote vote)
    {
        vote.VotedAt = DateTime.UtcNow;

        if (_context.Entry(vote).State == EntityState.Detached)
        {
            _context.CurrentPickVotes.Update(vote);
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateRoomAsync(CurrentPickRoom room)
    {
        room.UpdatedAt = DateTime.UtcNow;

        if (_context.Entry(room).State == EntityState.Detached)
        {
            _context.CurrentPickRooms.Update(room);
        }

        await _context.SaveChangesAsync();
    }
}
