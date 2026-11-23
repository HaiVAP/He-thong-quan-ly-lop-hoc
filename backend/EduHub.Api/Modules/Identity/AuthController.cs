using System.Security.Claims;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Identity;

[ApiController]
[Route("api/auth")]
public class AuthController(EduHubDbContext db, JwtTokenService tokens) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record RegisterRequest(string FullName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record ProfileRequest(string FullName, string? Phone);
    public record ChangePasswordRequest(string OldPassword, string NewPassword);

    /// <summary>
    /// FR-01: Đăng ký tài khoản mới (mặc định vai trò Sinh viên)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BadRequestException("Họ tên không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new BadRequestException("Mật khẩu phải có độ dài tối thiểu 8 ký tự.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email))
            throw new ConflictException("Email này đã được đăng ký trong hệ thống.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "Student",
            Status = "Active"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role
        }, "Đăng ký tài khoản thành công."));
    }

    /// <summary>
    /// FR-01: Đăng nhập vào hệ thống
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");

        if (user.Status != "Active")
            throw new ForbiddenException("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");

        var token = tokens.Create(user);

        return Ok(ApiResponse.Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.Status
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
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        return Ok(ApiResponse.Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Status,
            user.Phone
        }));
    }

    /// <summary>
    /// FR-02: Cập nhật thông tin cá nhân
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> Profile([FromBody] ProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BadRequestException("Họ tên không được để trống.");

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();

        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(new
        {
            user.Id,
            user.FullName,
            user.Phone
        }, "Cập nhật thông tin cá nhân thành công."));
    }

    /// <summary>
    /// FR-02: Đổi mật khẩu
    /// </summary>
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new BadRequestException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BadRequestException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đổi mật khẩu thành công."));
    }
}
