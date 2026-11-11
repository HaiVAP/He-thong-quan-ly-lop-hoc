using System.Security.Claims;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Identity;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(EduHubDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record UserDto(int Id, string FullName, string Email, string Role, string Status, string? Phone, DateTime CreatedAt);
    public record UpdateRoleRequest(string Role);
    public record UpdateStatusRequest(string Status);

    /// <summary>
    /// FR-03: Admin quản lý danh sách tài khoản người dùng (Tìm kiếm, phân trang, lọc theo vai trò và trạng thái)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? q,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
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

        return Ok(ApiResponse.Ok(new
        {
            items,
            totalItems,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        }));
    }

    /// <summary>
    /// Lấy chi tiết thông tin tài khoản người dùng
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (u is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        return Ok(ApiResponse.Ok(new UserDto(u.Id, u.FullName, u.Email, u.Role, u.Status, u.Phone, u.CreatedAt)));
    }

    /// <summary>
    /// FR-04: Admin phân quyền tài khoản (Admin, Lecturer, Student)
    /// </summary>
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var validRoles = new[] { "Admin", "Lecturer", "Student" };
        if (!validRoles.Contains(request.Role))
            throw new BadRequestException("Vai trò không hợp lệ. Chỉ chấp nhận Admin, Lecturer hoặc Student.");

        var user = await db.Users.FindAsync(id);
        if (user is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        if (user.Id == CurrentUserId && request.Role != "Admin")
            throw new BadRequestException("Bạn không thể tự hạ quyền Admin của chính mình.");

        user.Role = request.Role;
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.CreatedAt),
            "Cập nhật vai trò thành công."));
    }

    /// <summary>
    /// FR-04: Admin khóa hoặc mở khóa tài khoản
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var validStatuses = new[] { "Active", "Locked" };
        if (!validStatuses.Contains(request.Status))
            throw new BadRequestException("Trạng thái không hợp lệ. Chỉ chấp nhận Active hoặc Locked.");

        var user = await db.Users.FindAsync(id);
        if (user is null)
            throw new NotFoundException($"Không tìm thấy tài khoản có ID {id}.");

        if (user.Id == CurrentUserId)
            throw new BadRequestException("Bạn không thể tự khóa tài khoản của chính mình.");

        user.Status = request.Status;
        await db.SaveChangesAsync();

        var message = request.Status == "Locked" ? "Đã khóa tài khoản thành công." : "Đã mở khóa tài khoản thành công.";
        return Ok(ApiResponse.Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.Status, user.Phone, user.CreatedAt), message));
    }
}

