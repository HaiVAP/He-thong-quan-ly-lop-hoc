using EduHub.Identity.Application.DTOs;
using EduHub.Identity.Infrastructure;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Identity.Application.Services;

public class UserService(IdentityDbContext db) : IUserService
{
    public async Task<PagedUsersResult> GetUsersAsync(string? q, string? role, string? status, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 10;

        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.Status, u.Phone, u.CreatedAt))
            .ToListAsync();

        return new PagedUsersResult(items, totalItems, page, pageSize, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (u is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        return new UserDto(u.Id, u.FullName, u.Email, u.Role, u.Status, u.Phone, u.CreatedAt);
    }

    public async Task<UserDto> UpdateRoleAsync(int id, int currentUserId, string role)
    {
        var validRoles = new[] { "Admin", "Lecturer", "Student" };
        if (!validRoles.Contains(role))
            throw new BadRequestException("Vai trò không hợp lệ. Chỉ chấp nhận Admin, Lecturer hoặc Student.");

        var user = await db.Users.FindAsync(id);
        if (user is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        if (user.Id == currentUserId && role != "Admin")
            throw new BadRequestException("Bạn không thể tự hạ quyền Admin của chính mình.");

        user.Role = role;
        await db.SaveChangesAsync();

        return new UserDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.CreatedAt);
    }

    public async Task<UserDto> UpdateStatusAsync(int id, int currentUserId, string status)
    {
        var validStatuses = new[] { "Active", "Locked" };
        if (!validStatuses.Contains(status))
            throw new BadRequestException("Trạng thái không hợp lệ. Chỉ chấp nhận Active hoặc Locked.");

        var user = await db.Users.FindAsync(id);
        if (user is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        if (user.Id == currentUserId)
            throw new BadRequestException("Bạn không thể tự khóa tài khoản của chính mình.");

        user.Status = status;
        await db.SaveChangesAsync();

        return new UserDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.CreatedAt);
    }
}

