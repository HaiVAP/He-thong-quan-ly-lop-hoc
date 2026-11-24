namespace EduHub.Identity.Application.DTOs;

public record UserDto(int Id, string FullName, string Email, string Role, string Status, string? Phone, DateTime CreatedAt);
public record UpdateRoleRequest(string Role);
public record UpdateStatusRequest(string Status);
public record PagedUsersResult(List<UserDto> Items, int TotalItems, int Page, int PageSize, int TotalPages);

