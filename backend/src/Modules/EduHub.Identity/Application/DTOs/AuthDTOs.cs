namespace EduHub.Identity.Application.DTOs;

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ProfileRequest(string FullName, string? Phone);
public record ChangePasswordRequest(string OldPassword, string NewPassword);

public record UserProfileDto(int Id, string FullName, string Email, string Role, string Status, string? Phone, string? AvatarUrl);
public record LoginResponseDto(string Token, UserProfileDto User);
public record RegisterResponseDto(int Id, string FullName, string Email, string Role);

