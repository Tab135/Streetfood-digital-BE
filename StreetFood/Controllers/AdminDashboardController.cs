using BO.Common;
using BO.DTO.Dashboard;
using BO.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminDashboardController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService ?? throw new ArgumentNullException(nameof(adminDashboardService));
        }

        [HttpGet("user-signups")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdminUserSignupChartDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserSignupChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "fromDate and toDate are required." });
                }

                var dashboardDto = await _adminDashboardService.GetUserSignupChartAsync(fromDate, toDate);

                return Ok(new
                {
                    message = "Get user signup chart successfully",
                    data = dashboardDto
                });
            }
            catch (DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("money")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdminMoneyChartDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMoneyChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "fromDate and toDate are required." });
                }

                var dashboardDto = await _adminDashboardService.GetMoneyChartAsync(fromDate, toDate);

                return Ok(new
                {
                    message = "Get money chart successfully",
                    data = dashboardDto
                });
            }
            catch (DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("compensation")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdminCompensationChartDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCompensationChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "fromDate and toDate are required." });
                }

                var dashboardDto = await _adminDashboardService.GetCompensationChartAsync(fromDate, toDate);

                return Ok(new
                {
                    message = "Get compensation chart successfully",
                    data = dashboardDto
                });
            }
            catch (DomainExceptions ex)
            {
                return BadRequest(new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("user-to-vendor-conversions")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdminUserToVendorConversionChartDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserToVendorConversionChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "fromDate and toDate are required." });
                }

                var dashboardDto = await _adminDashboardService.GetUserToVendorConversionChartAsync(fromDate, toDate);

                return Ok(new
                {
                    message = "Get user to vendor conversion chart successfully",
                    data = dashboardDto
                });
            }
            catch (DomainExceptions ex)
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
