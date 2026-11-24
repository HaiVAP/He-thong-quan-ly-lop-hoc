using EduHub.Identity.Application.DTOs;

namespace EduHub.Identity.Application.Services;

public interface IUserService
{
    Task<PagedUsersResult> GetUsersAsync(string? q, string? role, string? status, int page, int pageSize);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> UpdateRoleAsync(int id, int currentUserId, string role);
    Task<UserDto> UpdateStatusAsync(int id, int currentUserId, string status);
}

