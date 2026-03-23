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
                RequiredTierId = dto.RequiredTierId,
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
                throw new DomainExceptions("Chi nh�nh d� tham gia chi?n d?ch n�y.");

            if (campaign.RequiredTierId.HasValue)
            {
                var targetTier = await _tierRepo.GetByIdAsync(campaign.RequiredTierId.Value);
                if (targetTier != null && branch.Tier != null && branch.Tier.Weight < targetTier.Weight)
                {
                    throw new DomainExceptions("C?p b?c c?a chi nh�nh kh�ng d? di?u ki?n tham gia chi?n d?ch n�y.");
                }
            }

            var joinRequest = new BranchCampaign
            {
                BranchId = branchId,
                CampaignId = campaignId,
                Status = "Pending"
            };
            await _branchCampaignRepo.CreateAsync(joinRequest);

            // T�ch h?p logic Payment n?u c� ph� (Mocking Participation Fee = 1,000,000)
            decimal participationFee = 1000000;
            var paymentId = await CreateFeePaymentAsync(userId, branchId, joinRequest.Id, participationFee);
            return paymentId; // Tr? v? PaymentId d? controller l?y Link thanh to�n
        }

        private async Task<int> CreateFeePaymentAsync(int userId, int branchId, int branchCampaignId, decimal feeAmount)
        {
            var orderCode = long.Parse(DateTime.UtcNow.ToString("yyMMddHHmmss") + userId);
            var savedPayment = await _paymentRepo.CreatePayment(
                userId: userId,
                orderCode: orderCode,
                branchId: branchId,
                amount: (int)feeAmount,
                description: "Tham gia chien dich",
                checkoutUrl: null,
                orderId: null,
                branchCampaignId: branchCampaignId
            );
            return savedPayment.Id;
        }
    }
}
