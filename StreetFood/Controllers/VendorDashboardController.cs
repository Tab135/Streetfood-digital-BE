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

        [HttpGet("revenue/bar")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<RevenueBarChartDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueBarChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
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

                var chart = await _vendorDashboardService.GetRevenueBarChartAsync(userId, fromDate, toDate);

                return Ok(new
                {
                    message = "Get revenue bar chart successfully",
                    data = chart
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
        public async Task<IActionResult> GetVoucherDashboard([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
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

                var dashboardDto = await _vendorDashboardService.GetVoucherDashboardAsync(userId, fromDate, toDate);
                
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

        [HttpGet("campaigns")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<CampaignDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCampaignDashboard([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
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

                var dashboardDto = await _vendorDashboardService.GetCampaignDashboardAsync(userId, fromDate, toDate);

                return Ok(new
                {
                    message = "Get campaign dashboard successfully",
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
        public async Task<IActionResult> GetDishDashboard([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
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

                var dashboardDto = await _vendorDashboardService.GetDishDashboardAsync(userId, fromDate, toDate);
                
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

        [HttpGet("branches/performance")]
        [Authorize(Roles = "Vendor")]
        [ProducesResponseType(typeof(ApiResponse<BranchesPerformanceDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchesPerformance([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
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

                var branchesPerformance = await _vendorDashboardService.GetBranchesPerformanceAsync(userId, fromDate, toDate);

                return Ok(new
                {
                    message = "Get branches performance successfully",
                    data = branchesPerformance
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
