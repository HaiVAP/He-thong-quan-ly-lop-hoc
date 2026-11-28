using System.Security.Claims;
using EduHub.Communication.Application.DTOs;
using EduHub.Communication.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Communication.Controllers;

[ApiController]
[Route("api/classes/{classId:int}/announcements")]
[Authorize]
public class AnnouncementsController(IAnnouncementService announcementService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// FR-29: Sinh viên & Giảng viên xem danh sách thông báo của lớp
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(int classId)
    {
        var result = await announcementService.ListByClassAsync(classId, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// FR-28: Giảng viên tạo và đăng thông báo mới trong lớp học
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(int classId, [FromBody] CreateAnnouncementRequest request)
    {
        var result = await announcementService.CreateAsync(classId, CurrentUserId, request);
        return Ok(ApiResponse.Ok(result, "Đăng thông báo thành công."));
    }
}

