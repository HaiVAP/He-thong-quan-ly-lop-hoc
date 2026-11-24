using EduHub.Identity.Application.Common;
using EduHub.Identity.Application.DTOs;
using EduHub.Identity.Domain;
using EduHub.Identity.Infrastructure;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Identity.Application.Services;

public class AuthService(IdentityDbContext db, JwtTokenService tokens) : IAuthService
{
    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequest request)
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

        return new RegisterResponseDto(user.Id, user.FullName, user.Email, user.Role);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");

        if (user.Status != "Active")
            throw new ForbiddenException("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");

        var token = tokens.Create(user);
        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.AvatarUrl);

        return new LoginResponseDto(token, profile);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        return new UserProfileDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.AvatarUrl);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, ProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BadRequestException("Họ tên không được để trống.");

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();

        await db.SaveChangesAsync();

        return new UserProfileDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.AvatarUrl);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new BadRequestException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            throw new NotFoundException("Không tìm thấy thông tin tài khoản.");

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BadRequestException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync();
    }
}

