using BO.DTO.Campaigns;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepo;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly ITierRepository _tierRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IVendorRepository _vendorRepo;

        public CampaignService(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            IBranchRepository branchRepo,
            ITierRepository tierRepo,
            IPaymentRepository paymentRepo,
            IVendorRepository vendorRepo)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _branchRepo = branchRepo;
            _tierRepo = tierRepo;
            _paymentRepo = paymentRepo;
            _vendorRepo = vendorRepo;
        }

        public async Task CreateSystemCampaignAsync(CreateCampaignDto dto)
        {
            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                RegistrationStartDate = dto.RegistrationStartDate,
                RegistrationEndDate = dto.RegistrationEndDate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedByBranchId = null
            };
            await _campaignRepo.CreateAsync(campaign);
        }

        public async Task CreateRestaurantCampaignAsync(int userId, int branchId, CreateCampaignDto dto)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Kh�ng t�m th?y Vendor c?a ngu?i d�ng n�y.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null || branch.VendorId != vendor.VendorId)
                throw new DomainExceptions("Kh�ng t�m th?y chi nh�nh ho?c kh�ng c� quy?n truy c?p.");

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedByBranchId = branchId
            };
            await _campaignRepo.CreateAsync(campaign);
        }

        public async Task CreateVendorCampaignAsync(int userId, CreateCampaignDto dto)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedByVendorId = vendor.VendorId
            };
            await _campaignRepo.CreateAsync(campaign);
        }

        public async Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Kh�ng t�m th?y Vendor c?a ngu?i d�ng n�y.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null || branch.VendorId != vendor.VendorId)
                throw new DomainExceptions("Kh�ng t�m th?y chi nh�nh ho?c kh�ng c� quy?n truy c?p.");

            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByBranchId != null)
                throw new DomainExceptions("Chi?n d?ch h? th?ng kh�ng t?n t?i.");

            var now = DateTime.UtcNow;
            if (campaign.RegistrationStartDate.HasValue && campaign.RegistrationEndDate.HasValue)
            {
                if (now < campaign.RegistrationStartDate || now > campaign.RegistrationEndDate)
                    throw new DomainExceptions("Kh�ng n?m trong th?i gian tham gia chi?n d?ch n�y.");
            }

            var existingJoin = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaignId);
            if (existingJoin != null)
                throw new DomainExceptions("Chi nhánh dã tham gia chiến dịch này.");

            // Require minimum Tier for System Campaign (Weight >= 1)
            if (campaign.CreatedByBranchId == null && campaign.CreatedByVendorId == null)
            {
                if (branch.Tier == null || branch.Tier.Weight < 1)
                {
                    throw new DomainExceptions("Cấp bậc của chi nhánh không đủ điều kiện tham gia chiến dịch system (yêu cầu Tier mặc định trở lên).");
                }
            }

            var joinRequest = new BranchCampaign
            {
                BranchId = branchId,
                CampaignId = campaignId,
                Status = "Pending"
            };
            await _branchCampaignRepo.CreateAsync(joinRequest);

            return joinRequest.Id;
        }
    }
}


