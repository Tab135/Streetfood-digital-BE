using BO.DTO.Voucher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace StreetFood.Controllers;

[Route("api/vouchers")]
[ApiController]
public class VoucherController : ControllerBase
{
    private readonly IVoucherService _voucherService;

    public VoucherController(IVoucherService voucherService)
    {
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator,Vendor")]
    public async Task<IActionResult> Create([FromBody] List<CreateVoucherDto> createDtos)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Model is not valid" });
        }

        if (createDtos == null || createDtos.Count == 0)
        {
            return BadRequest(new { message = "At least one voucher is required" });
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var created = await _voucherService.CreateVouchersAsync(createDtos, userId);
        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Vouchers created successfully",
            data = created
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var voucher = await _voucherService.GetVoucherByIdAsync(id);
        if (voucher == null)
        {
            return NotFound(new { message = "Voucher not found" });
        }

        return Ok(voucher);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isBelongAQuestTask = null, [FromQuery] bool? isRemaining = null, [FromQuery] bool? isSystemVoucher = null)
    {
        var vouchers = await _voucherService.GetAllVouchersAsync(isBelongAQuestTask, isRemaining, isSystemVoucher);
        return Ok(vouchers);
    }

    [HttpGet("marketplace")]
    public async Task<IActionResult> GetMarketplace()
    {
        var vouchers = await _voucherService.GetMarketplaceVouchersAsync();
        return Ok(new
        {
            message = "Marketplace vouchers retrieved successfully",
            data = vouchers
        });
    }

    [HttpGet("mine")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var vouchers = await _voucherService.GetUserVouchersAsync(userId);
        return Ok(new
        {
            message = "User vouchers retrieved successfully",
            data = vouchers
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Moderator,Vendor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVoucherDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Model is not valid" });
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var updated = await _voucherService.UpdateVoucherAsync(id, updateDto, userId);
        return Ok(updated);
    }

    [HttpGet("campaign/{campaignId}")]
    public async Task<IActionResult> GetByCampaignId(int campaignId)
    {
        var vouchers = await _voucherService.GetVouchersByCampaignIdAsync(campaignId);
        return Ok(vouchers);
    }

    [HttpGet("mine/branch/{branchId}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetApplicable(int branchId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var vouchers = await _voucherService.GetApplicableUserVouchersAsync(userId, branchId);
        return Ok(new
        {
            message = "Applicable vouchers retrieved successfully",
            data = vouchers
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Moderator,Vendor")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        await _voucherService.DeleteVoucherAsync(id, userId);
        return Ok(new { message = "Voucher deleted successfully" });
    }

    [HttpPost("{id}/claim")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Claim(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var claimed = await _voucherService.ClaimVoucherAsync(id, userId);
        return Ok(new
        {
            message = "Voucher claimed successfully",
            data = claimed
        });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
    }
}
