using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.GhostPin;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;


namespace Service
{
    public class GhostPinService : IGhostPinService
    {
        private readonly IGhostPinRepository _ghostPinRepo;
        private readonly IBranchRepository _branchRepo;

        public GhostPinService(IGhostPinRepository ghostPinRepo, IBranchRepository branchRepo)
        {
            _ghostPinRepo = ghostPinRepo;
            _branchRepo = branchRepo;
        }

        public async Task<GhostPinResponseDto> CreateGhostPinAsync(int creatorId, CreateGhostPinRequest request)
        {
            var ghostPin = new GhostPin
            {
                CreatorId = creatorId,
                Name = request.Name,
                AddressDetail = request.AddressDetail, Ward = request.Ward, City = request.City,
                Lat = request.Lat,
                Long = request.Long,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            await _ghostPinRepo.CreateAsync(ghostPin);
            return MapToDto(ghostPin);
        }

        public async Task<GhostPinResponseDto> GetGhostPinByIdAsync(int id, int userId, string role)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> ApproveGhostPinAsync(int id)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.IsVerified)
                throw new Exception("Pin is already verified.");

            // Reset any reject reasons if it's being "approved" for further audit
            pin.RejectReason = null;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> RejectGhostPinAsync(int id, RejectGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.IsVerified)
                throw new Exception("Verified pins cannot be rejected.");

            pin.RejectReason = request.Reason;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<GhostPinResponseDto> AuditGhostPinAsync(int id, AuditGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (pin.IsVerified)
                throw new Exception("Ghost pin is already verified.");
            
            if (!string.IsNullOrEmpty(pin.RejectReason))
                throw new Exception("Ghost pin was rejected.");

            double dist = CalculateDistance(pin.Lat, pin.Long, request.ModLat, request.ModLong);
            if (dist > 50.0) 
            {
                throw new Exception($"Moderator is too far from location. Distance: {dist:F1}m (Max allowed: 50m)");
            }

            var newBranch = new Branch
            {
                Name = pin.Name,
                AddressDetail = pin.AddressDetail, Ward = pin.Ward, City = pin.City,
                PhoneNumber = null,
                Email = null,
                Lat = pin.Lat,
                Long = pin.Long,
                VendorId = null,
                IsVerified = true,
                AvgRating = 0,
                TotalReviewCount = 0,
                TotalRatingSum = 0,
                BatchReviewCount = 0,
                BatchRatingSum = 0,
                TierId = 2, // DEFAULT SILVER
                CreatedAt = DateTime.UtcNow
            };

            await _branchRepo.CreateAsync(newBranch);

            pin.IsVerified = true;
            pin.LinkedBranchId = newBranch.BranchId;
            pin.UpdatedAt = DateTime.UtcNow;
            await _ghostPinRepo.UpdateAsync(pin);

            return MapToDto(pin);
        }

        public async Task<object> ClaimGhostPinAsync(int id, int vendorId, ClaimGhostPinRequest request)
        {
            var pin = await _ghostPinRepo.GetByIdAsync(id);
            if (pin == null) throw new Exception("Ghost pin not found");

            if (!pin.IsVerified || pin.LinkedBranchId == null)
                throw new Exception("Only verified and audited pins can be claimed.");

            var unownedBranch = await _branchRepo.GetByIdAsync(pin.LinkedBranchId.Value);
            if (unownedBranch != null && unownedBranch.VendorId != null)
                throw new Exception("This branch has already been claimed.");

            if (request.ExistingBranchId.HasValue)
            {
                var branch = await _branchRepo.GetByIdAsync(request.ExistingBranchId.Value);
                if (branch == null || branch.VendorId != vendorId)
                    throw new Exception("Invalid or unauthorized existing branch.");
                
                branch.Lat = pin.Lat;
                branch.Long = pin.Long;
                branch.AddressDetail = pin.AddressDetail; branch.Ward = pin.Ward; branch.City = pin.City;
                await _branchRepo.UpdateAsync(branch);

                if (pin.LinkedBranchId.HasValue && pin.LinkedBranchId.Value != branch.BranchId)
                {
                    await _branchRepo.DeleteAsync(pin.LinkedBranchId.Value);
                }

                pin.LinkedBranchId = branch.BranchId;
                pin.UpdatedAt = DateTime.UtcNow;
                await _ghostPinRepo.UpdateAsync(pin);

                return new { Message = "Merged with existing branch. Generating payment link...", BranchId = branch.BranchId };
            }
            else
            {
                pin.UpdatedAt = DateTime.UtcNow;
                await _ghostPinRepo.UpdateAsync(pin);
                return new { Message = "Claiming new branch. Generating payment link...", BranchId = pin.LinkedBranchId.Value };
            }
        }

        public async Task<IEnumerable<GhostPinResponseDto>> GetAllGhostPinsAsync()
        {
            var pins = await _ghostPinRepo.GetAllAsync();
            return pins.Select(MapToDto).ToList();
        }

        private GhostPinResponseDto MapToDto(GhostPin p)
        {
            return new GhostPinResponseDto
            {
                GhostPinId = p.GhostPinId,
                CreatorId = p.CreatorId,
                Name = p.Name,
                AddressDetail = p.AddressDetail, Ward = p.Ward, City = p.City,
                Lat = p.Lat,
                Long = p.Long,
                IsVerified = p.IsVerified,
                AvgRating = p.AvgRating,
                TotalReviewCount = p.TotalReviewCount,
                TotalRatingSum = p.TotalRatingSum,
                BatchReviewCount = p.BatchReviewCount,
                BatchRatingSum = p.BatchRatingSum,
                TierId = p.TierId,
                LastTierResetAt = p.LastTierResetAt,
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
