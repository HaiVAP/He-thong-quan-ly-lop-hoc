using System.Security.Claims;
using EduHub.Identity.Application.DTOs;
using EduHub.Identity.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// FR-01: Đăng ký tài khoản mới (mặc định vai trò Sinh viên)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(ApiResponse.Ok(result, "Đăng ký tài khoản thành công."));
    }

    /// <summary>
    /// FR-01: Đăng nhập vào hệ thống
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse.Ok(new
        {
            token = result.Token,
            user = new
            {
                result.User.Id,
                result.User.FullName,
                result.User.Email,
                result.User.Role,
                result.User.Status
            }
        }, "Đăng nhập thành công."));
    }

    /// <summary>
    /// FR-01: Xem thông tin người dùng hiện tại
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var profile = await authService.GetProfileAsync(CurrentUserId);
        return Ok(ApiResponse.Ok(new
        {
            profile.Id,
            profile.FullName,
            profile.Email,
            profile.Role,
            profile.Status,
            profile.Phone
        }));
    }

    /// <summary>
    /// FR-02: Cập nhật thông tin cá nhân
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> Profile([FromBody] ProfileRequest request)
    {
        var profile = await authService.UpdateProfileAsync(CurrentUserId, request);
        return Ok(ApiResponse.Ok(new
        {
            profile.Id,
            profile.FullName,
            profile.Phone
        }, "Cập nhật thông tin cá nhân thành công."));
    }

    /// <summary>
    /// FR-02: Đổi mật khẩu
    /// </summary>
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await authService.ChangePasswordAsync(CurrentUserId, request);
        return Ok(ApiResponse.Ok("Đổi mật khẩu thành công."));
    }
}

