using BO.DTO.Auth;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Interfaces;
using Service.JWT;
using StreetFood.Controllers;
using System.Security.Claims;

namespace StreetFood.Tests.Auth;

public class AuthControllerTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Build a controller with mocked dependencies.</summary>
    private static (AuthController controller, Mock<IUserService> userSvc, Mock<IJwtService> jwtSvc)
        BuildController(ClaimsPrincipal? user = null)
    {
        var userSvc = new Mock<IUserService>();
        var jwtSvc = new Mock<IJwtService>();

        var controller = new AuthController(userSvc.Object, jwtSvc.Object);

        // Wire up a fake HttpContext so controller.User works in protected-endpoint tests
        var httpCtx = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal()
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        return (controller, userSvc, jwtSvc);
    }

    /// <summary>Build a ClaimsPrincipal that mimics a valid JWT identity.</summary>
    private static ClaimsPrincipal BuildAuthenticatedUser(int userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, Role.User.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    /// <summary>Create a sample User entity for use across tests.</summary>
    private static User MakeSampleUser(int id = 1) => new User
    {
        Id = id,
        UserName = "testuser",
        Email = "test@example.com",
        FirstName = "John",
        LastName = "Doe",
        Role = Role.User,
        PhoneNumber = "0900000001",
        AvatarUrl = "https://example.com/avatar.jpg",
        Point = 100,
        XP = 50,
        MoneyBalance = 999m,
        TierId = 2,
        Status = "active",
        UserInfoSetup = true,
        DietarySetup = false,
    };

    // ─── POST /api/auth/google-login ──────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_ValidToken_Returns200WithUserAndToken()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new GoogleAuthDto { IdToken = "valid_google_id_token" };
        var expectedUser = MakeSampleUser();
        var loginResponse = new LoginResponse { Token = "jwt_token_abc", User = expectedUser };

        userSvc.Setup(s => s.GoogleLoginAsync(dto)).ReturnsAsync(loginResponse);

        // Act
        var result = await controller.GoogleLogin(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        // Verify the response shape contains required fields
        var body = ok.Value!;
        var messageProperty = body.GetType().GetProperty("message")?.GetValue(body) as string;
        var tokenProperty = body.GetType().GetProperty("token")?.GetValue(body) as string;

        Assert.Equal("Login successful", messageProperty);
        Assert.Equal("jwt_token_abc", tokenProperty);
    }

    [Fact]
    public async Task GoogleLogin_InvalidGoogleToken_Returns401()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new GoogleAuthDto { IdToken = "bad_token" };

        userSvc.Setup(s => s.GoogleLoginAsync(dto))
               .ThrowsAsync(new InvalidJwtException("Token is invalid"));

        // Act
        var result = await controller.GoogleLogin(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);

        var body = unauthorized.Value!;
        var msg = body.GetType().GetProperty("message")?.GetValue(body) as string;
        Assert.Equal("Invalid Google token", msg);
    }

    [Fact]
    public async Task GoogleLogin_ServiceThrowsGenericException_Returns400()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new GoogleAuthDto { IdToken = "any_token" };

        userSvc.Setup(s => s.GoogleLoginAsync(dto))
               .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await controller.GoogleLogin(dto);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    // ─── POST /api/auth/facebook-login ───────────────────────────────────────

    [Fact]
    public async Task FacebookLogin_ValidAccessToken_Returns200WithUserAndToken()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new FacebookAuthDto { AccessToken = "fb_access_token" };
        var expectedUser = MakeSampleUser();
        var loginResponse = new LoginResponse { Token = "fb_jwt_token", User = expectedUser };

        userSvc.Setup(s => s.FacebookLoginAsync(dto)).ReturnsAsync(loginResponse);

        var result = await controller.FacebookLogin(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = ok.Value!;
        var token = body.GetType().GetProperty("token")?.GetValue(body) as string;
        Assert.Equal("fb_jwt_token", token);
    }

    [Fact]
    public async Task FacebookLogin_ServiceThrows_Returns400()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new FacebookAuthDto { AccessToken = "bad_token" };

        userSvc.Setup(s => s.FacebookLoginAsync(dto))
               .ThrowsAsync(new Exception("Facebook auth failed"));

        // Act
        var result = await controller.FacebookLogin(dto);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = bad.Value!;
        var msg = body.GetType().GetProperty("message")?.GetValue(body) as string;
        Assert.Equal("Facebook auth failed", msg);
    }

    // ─── POST /api/auth/phone-login ───────────────────────────────────────────

    [Fact]
    public async Task PhoneLogin_ValidPhoneNumber_Returns200WithOtp()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new PhoneLoginDto { PhoneNumber = "0900000001" };

        userSvc.Setup(s => s.SendPhoneLoginOtpAsync("0900000001"))
               .ReturnsAsync("123456");

        // Act
        var result = await controller.PhoneLogin(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = ok.Value!;
        var otp = body.GetType().GetProperty("otp")?.GetValue(body) as string;
        var phone = body.GetType().GetProperty("phoneNumber")?.GetValue(body) as string;

        Assert.Equal("123456", otp);
        Assert.Equal("0900000001", phone);
    }

    [Fact]
    public async Task PhoneLogin_UnregisteredPhone_Returns400()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new PhoneLoginDto { PhoneNumber = "0000000000" };

        userSvc.Setup(s => s.SendPhoneLoginOtpAsync("0000000000"))
               .ThrowsAsync(new Exception("Phone number not found"));

        // Act
        var result = await controller.PhoneLogin(dto);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = bad.Value!;
        var msg = body.GetType().GetProperty("message")?.GetValue(body) as string;
        Assert.Equal("Phone number not found", msg);
    }

    // ─── POST /api/auth/phone-verify ──────────────────────────────────────────

    [Fact]
    public async Task PhoneVerify_CorrectOtp_Returns200WithToken()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new VerifyPhoneOtpDto { PhoneNumber = "0900000001", Otp = "123456" };
        var expectedUser = MakeSampleUser();
        var loginResponse = new LoginResponse { Token = "otp_jwt_token", User = expectedUser };

        userSvc.Setup(s => s.VerifyPhoneOtpAsync("0900000001", "123456"))
               .ReturnsAsync(loginResponse);

        // Act
        var result = await controller.PhoneVerify(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = ok.Value!;
        var token = body.GetType().GetProperty("token")?.GetValue(body) as string;
        Assert.Equal("otp_jwt_token", token);
    }

    [Fact]
    public async Task PhoneVerify_WrongOtp_Returns401()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController();
        var dto = new VerifyPhoneOtpDto { PhoneNumber = "0900000001", Otp = "999999" };

        userSvc.Setup(s => s.VerifyPhoneOtpAsync("0900000001", "999999"))
               .ThrowsAsync(new Exception("Invalid or expired OTP"));

        // Act
        var result = await controller.PhoneVerify(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);

        var body = unauthorized.Value!;
        var msg = body.GetType().GetProperty("message")?.GetValue(body) as string;
        Assert.Equal("Invalid or expired OTP", msg);
    }

    // ─── GET /api/auth/profile ────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_AuthenticatedUser_Returns200WithProfile()
    {
        // Arrange
        var user = MakeSampleUser(42);
        var principal = BuildAuthenticatedUser(42);
        var (controller, userSvc, _) = BuildController(principal);

        userSvc.Setup(s => s.GetUserById(42)).ReturnsAsync(user);

        // Act
        var result = await controller.GetProfile();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = ok.Value!;
        var userId = (int?)body.GetType().GetProperty("userId")?.GetValue(body);
        var email = body.GetType().GetProperty("email")?.GetValue(body) as string;

        Assert.Equal(42, userId);
        Assert.Equal("test@example.com", email);
    }

    [Fact]
    public async Task GetProfile_MissingClaim_Returns401()
    {
        // Arrange — principal has NO NameIdentifier claim
        var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var (controller, _, _) = BuildController(emptyPrincipal);

        // Act
        var result = await controller.GetProfile();

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetProfile_NonNumericUserIdClaim_Returns401()
    {
        // Arrange — NameIdentifier is not a valid integer
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-an-int") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        var (controller, _, _) = BuildController(principal);

        // Act
        var result = await controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_ServiceThrows_Returns400()
    {
        // Arrange
        var principal = BuildAuthenticatedUser(1);
        var (controller, userSvc, _) = BuildController(principal);

        userSvc.Setup(s => s.GetUserById(1))
               .ThrowsAsync(new Exception("User not found"));

        // Act
        var result = await controller.GetProfile();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── PUT /api/auth/profile ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ValidRequest_Returns200WithUpdatedUser()
    {
        // Arrange
        var principal = BuildAuthenticatedUser(5);
        var (controller, userSvc, _) = BuildController(principal);

        var updateDto = new UpdateUserProfileDto
        {
            Username = "newname",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var updatedUser = MakeSampleUser(5);
        updatedUser.UserName = "newname";
        updatedUser.FirstName = "Jane";
        updatedUser.LastName = "Smith";

        userSvc.Setup(s => s.UpdateUserProfile(5, updateDto)).ReturnsAsync(updatedUser);

        // Act
        var result = await controller.UpdateProfile(updateDto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = ok.Value!;
        var username = body.GetType().GetProperty("username")?.GetValue(body) as string;
        Assert.Equal("newname", username);
    }

    [Fact]
    public async Task UpdateProfile_MissingUserIdClaim_Returns401()
    {
        // Arrange — unauthenticated principal
        var (controller, _, _) = BuildController(new ClaimsPrincipal(new ClaimsIdentity()));
        var updateDto = new UpdateUserProfileDto { Username = "nobody" };

        // Act
        var result = await controller.UpdateProfile(updateDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ServiceThrows_Returns400()
    {
        // Arrange
        var principal = BuildAuthenticatedUser(5);
        var (controller, userSvc, _) = BuildController(principal);
        var updateDto = new UpdateUserProfileDto { Email = "duplicate@email.com" };

        userSvc.Setup(s => s.UpdateUserProfile(5, updateDto))
               .ThrowsAsync(new Exception("Email already in use"));

        // Act
        var result = await controller.UpdateProfile(updateDto);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = bad.Value!;
        var msg = body.GetType().GetProperty("message")?.GetValue(body) as string;
        Assert.Equal("Email already in use", msg);
    }

    // ─── Service Layer Delegation checks ─────────────────────────────────────
    // Verify the controller calls the service exactly once with the correct args.

    [Fact]
    public async Task GoogleLogin_CallsGoogleLoginAsync_ExactlyOnce()
    {
        var (controller, userSvc, _) = BuildController();
        var dto = new GoogleAuthDto { IdToken = "token" };
        userSvc.Setup(s => s.GoogleLoginAsync(dto))
               .ReturnsAsync(new LoginResponse { Token = "t", User = MakeSampleUser() });

        await controller.GoogleLogin(dto);

        userSvc.Verify(s => s.GoogleLoginAsync(dto), Times.Once);
    }

    [Fact]
    public async Task PhoneLogin_CallsSendPhoneLoginOtpAsync_ExactlyOnce()
    {
        var (controller, userSvc, _) = BuildController();
        var dto = new PhoneLoginDto { PhoneNumber = "0900000001" };
        userSvc.Setup(s => s.SendPhoneLoginOtpAsync("0900000001")).ReturnsAsync("111111");

        await controller.PhoneLogin(dto);

        userSvc.Verify(s => s.SendPhoneLoginOtpAsync("0900000001"), Times.Once);
    }

    [Fact]
    public async Task PhoneVerify_CallsVerifyPhoneOtpAsync_ExactlyOnce()
    {
        var (controller, userSvc, _) = BuildController();
        var dto = new VerifyPhoneOtpDto { PhoneNumber = "0908835619", Otp = "111111" };
        userSvc.Setup(s => s.VerifyPhoneOtpAsync("0908835619", "111111"))
               .ReturnsAsync(new LoginResponse { Token = "t", User = MakeSampleUser() });

        await controller.PhoneVerify(dto);

        userSvc.Verify(s => s.VerifyPhoneOtpAsync("0908835619", "111111"), Times.Once);
    }

    [Fact]
    public async Task GetProfile_CallsGetUserById_ExactlyOnce()
    {
        var principal = BuildAuthenticatedUser(10);
        var (controller, userSvc, _) = BuildController(principal);
        userSvc.Setup(s => s.GetUserById(10)).ReturnsAsync(MakeSampleUser(10));

        await controller.GetProfile();

        userSvc.Verify(s => s.GetUserById(10), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var (controller, userSvc, _) = BuildController(BuildAuthenticatedUser(5));
        
        userSvc.Setup(s => s.UpdateUserProfile(It.IsAny<int>(), It.IsAny<UpdateUserProfileDto>()))
               .ReturnsAsync(MakeSampleUser(5));

        controller.ModelState.AddModelError("Username", "Username is required");
        var updateDto = new UpdateUserProfileDto();

        var result = await controller.UpdateProfile(updateDto);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
