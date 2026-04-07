using BO.Common;
using BO.DTO.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorDashboardController : ControllerBase
    {
        private readonly IVendorDashboardService _vendorDashboardService;

        public VendorDashboardController(IVendorDashboardService vendorDashboardService)
        {
            _vendorDashboardService = vendorDashboardService ?? throw new ArgumentNullException(nameof(vendorDashboardService));
        }

        [HttpGet("revenue")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<RevenueDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueDashboard([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "fromDate and toDate are required." });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var dashboardDto = await _vendorDashboardService.GetRevenueDashboardAsync(userId, fromDate, toDate);
                
                return Ok(new
                {
                    message = "Get revenue dashboard successfully",
                    data = dashboardDto
                });
            }
            catch (BO.Exceptions.DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("vouchers")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<VoucherDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVoucherDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var dashboardDto = await _vendorDashboardService.GetVoucherDashboardAsync(userId);
                
                return Ok(new
                {
                    message = "Get voucher dashboard successfully",
                    data = dashboardDto
                });
            }
            catch (BO.Exceptions.DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("dishes")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<DishDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDishDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var dashboardDto = await _vendorDashboardService.GetDishDashboardAsync(userId);
                
                return Ok(new
                {
                    message = "Get dish dashboard successfully",
                    data = dashboardDto
                });
            }
            catch (BO.Exceptions.DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
