using BO.DTO.GhostPin;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IGhostPinService
    {
        Task<GhostPinResponseDto> CreateGhostPinAsync(int creatorId, CreateGhostPinRequest request);
        Task<GhostPinResponseDto> GetGhostPinByIdAsync(int id, int userId, string userRole);
        Task<GhostPinResponseDto> ApproveGhostPinAsync(int id);
        Task<GhostPinResponseDto> RejectGhostPinAsync(int id, RejectGhostPinRequest request);
        Task<GhostPinResponseDto> AuditGhostPinAsync(int id, AuditGhostPinRequest request);
        Task<object> ClaimGhostPinAsync(int id, int vendorId, ClaimGhostPinRequest request);
    }
}
