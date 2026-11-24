using EduHub.Identity.Application.DTOs;

namespace EduHub.Identity.Application.Services;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequest request);
    Task<LoginResponseDto> LoginAsync(LoginRequest request);
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, ProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

