using System.Security.Claims;
using EduHub.Identity.Application.DTOs;
using EduHub.Identity.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Identity.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService userService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        var result = await userService.GetUsersAsync(q, role, status, page, pageSize);
        return Ok(ApiResponse.Ok(new
        {
            items = result.Items,
            totalItems = result.TotalItems,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        }));
    }

    /// <summary>
    /// Lấy chi tiết thông tin tài khoản người dùng
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return Ok(ApiResponse.Ok(user));
    }

    /// <summary>
    /// FR-04: Admin phân quyền tài khoản (Admin, Lecturer, Student)
    /// </summary>
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var user = await userService.UpdateRoleAsync(id, CurrentUserId, request.Role);
        return Ok(ApiResponse.Ok(user, "Cập nhật vai trò thành công."));
    }

    /// <summary>
    /// FR-04: Admin khóa hoặc mở khóa tài khoản
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var user = await userService.UpdateStatusAsync(id, CurrentUserId, request.Status);
        var message = request.Status == "Locked" ? "Đã khóa tài khoản thành công." : "Đã mở khóa tài khoản thành công.";
        return Ok(ApiResponse.Ok(user, message));
    }
}

