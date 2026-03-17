using BO.DTO.GhostPin;
using BO.Entities;
using BO.Enums;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class GhostPinService : IGhostPinService
    {
        private readonly IGhostPinRepository _ghostPinRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IVendorRepository _vendorRepo;

        public GhostPinService(
            IGhostPinRepository ghostPinRepo,
            IBranchRepository branchRepo,
            IVendorRepository vendorRepo)
        {
            _ghostPinRepo = ghostPinRepo;
            _branchRepo = branchRepo;
            _vendorRepo = vendorRepo;
        }

        public async Task<GhostPinResponseDto> CreateGhostPinAsync(int creatorId, CreateGhostPinRequest request)
        {
            var pin = new GhostPin
            {
                CreatorId = creatorId,
                Name = request.Name,
                Address = request.Address,
                Lat = request.Lat,
                Long = request.Long,
                Status = GhostPinStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _ghostPinRepo.CreateAsync(pin);
            return MapToDto(created);
        }

        public async Task<GhostPinResponseDto> GetGhostPinByIdAsync(int id, int userId, string userRole)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (userRole != "Moderator" && pin.CreatorId != userId)
                throw new Exception("Access denied.");

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> ApproveGhostPinAsync(int id)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.Status != GhostPinStatusEnum.Pending)
                throw new Exception("Only pending pins can be approved.");

            pin.Status = GhostPinStatusEnum.Approved;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> RejectGhostPinAsync(int id, RejectGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.Status != GhostPinStatusEnum.Pending)
                throw new Exception("Only pending pins can be rejected.");

            pin.Status = GhostPinStatusEnum.Rejected;
            pin.RejectReason = request.Reason;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> AuditGhostPinAsync(int id, AuditGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.Status != GhostPinStatusEnum.Approved)
                throw new Exception("Ghost pin must be approved before audit.");

            double dist = CalculateDistance(pin.Lat, pin.Long, request.ModLat, request.ModLong);
            if (dist > 10.0)
                throw new Exception($"Moderator is too far from location (distance: {dist} m).");

            var newBranch = new Branch
            {
                Name = pin.Name,
                AddressDetail = pin.Address,
                City = "Unknown", // Will be filled by vendor later
                Lat = pin.Lat,
                Long = pin.Long,
                VendorId = null, // Ownerless
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _branchRepo.CreateAsync(newBranch);

            pin.Status = GhostPinStatusEnum.Verified;
            pin.LinkedBranchId = newBranch.BranchId;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<object> ClaimGhostPinAsync(int id, int vendorId, ClaimGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.Status != GhostPinStatusEnum.Verified)
                throw new Exception("Only verified pins can be claimed.");

            if (request.ExistingBranchId.HasValue)
            {
                var branch = await _branchRepo.GetByIdAsync(request.ExistingBranchId.Value);
                if (branch == null || branch.VendorId != vendorId)
                    throw new Exception("Invalid Branch ID.");

                branch.Lat = pin.Lat;
                branch.Long = pin.Long;
                branch.AddressDetail = pin.Address;
                await _branchRepo.UpdateAsync(branch);

                if (pin.LinkedBranchId.HasValue)
                {
                    await _branchRepo.DeleteAsync(pin.LinkedBranchId.Value);
                }

                pin.Status = GhostPinStatusEnum.Claimed;
                pin.LinkedBranchId = branch.BranchId;
                pin.UpdatedAt = DateTime.UtcNow;
                await _ghostPinRepo.UpdateAsync(pin);

                return new { Message = "Merged with existing branch", BranchId = branch.BranchId };
            }
            else
            {
                pin.Status = GhostPinStatusEnum.Claimed;
                pin.UpdatedAt = DateTime.UtcNow;
                await _ghostPinRepo.UpdateAsync(pin);

                return new { Message = "Claimed successfully, proceed to payment", BranchId = pin.LinkedBranchId };
            }
        }

        private GhostPinResponseDto MapToDto(GhostPin p)
        {
            return new GhostPinResponseDto
            {
                GhostPinId = p.GhostPinId,
                CreatorId = p.CreatorId,
                Name = p.Name,
                Address = p.Address,
                Lat = p.Lat,
                Long = p.Long,
                Status = p.Status.ToString().ToLower(),
                RejectReason = p.RejectReason,
                LinkedBranchId = p.LinkedBranchId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
