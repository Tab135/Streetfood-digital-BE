using BO.DTO.CurrentPick;

namespace Service.Interfaces;

public interface ICurrentPickService
{
    Task<CurrentPickRoomResponseDto> CreateRoomAsync(int hostUserId, CreateCurrentPickRoomDto dto);
    Task<CurrentPickRoomResponseDto> JoinRoomAsync(int userId, JoinCurrentPickRoomDto dto);
    Task<CurrentPickRoomResponseDto> GetRoomAsync(int roomId, int userId);
    Task<CurrentPickBranchDto> AddBranchAsync(int roomId, int userId, AddCurrentPickBranchDto dto);
    Task<CurrentPickRoomResponseDto> VoteAsync(int roomId, int userId, VoteCurrentPickDto dto);
    Task<FinalizedCurrentPickDto> FinalizeAsync(int roomId, int userId, FinalizeCurrentPickDto dto);
    Task<CurrentPickShareLinkDto> GetShareLinkAsync(int roomId, int userId);
    Task ClearRoomAsync(int roomId, int userId);
}
