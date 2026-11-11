using System.Security.Claims;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Communication;

[ApiController]
[Route("api/classes/{classId:int}/announcements")]
[Authorize]
public class AnnouncementsController(EduHubDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record CreateAnnouncementRequest(string Title, string Content);

    /// <summary>
    /// FR-29: Sinh viên & Giảng viên xem danh sách thông báo của lớp
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(int classId)
    {
        var classRoom = await db.Classes.FindAsync(classId);
        if (classRoom is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        var canAccess = User.IsInRole("Admin") ||
            classRoom.LecturerId == CurrentUserId ||
            await db.ClassMembers.AnyAsync(x => x.ClassRoomId == classId && x.StudentId == CurrentUserId && x.Status == "Active");

        if (!canAccess)
            throw new ForbiddenException("Bạn không có quyền xem thông báo của lớp học này.");

        var announcements = await db.Announcements
            .AsNoTracking()
            .Include(x => x.Author)
            .Where(x => x.ClassRoomId == classId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Content,
                x.CreatedAt,
                Author = new { x.Author.Id, x.Author.FullName, x.Author.Email }
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(announcements));
    }

    /// <summary>
    /// FR-28: Giảng viên tạo và đăng thông báo mới trong lớp học
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(int classId, [FromBody] CreateAnnouncementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            throw new BadRequestException("Tiêu đề và Nội dung thông báo không được để trống.");

        var classRoom = await db.Classes.FindAsync(classId);
        if (classRoom is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (classRoom.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể tạo thông báo cho lớp học do mình phụ trách.");

        var announcement = new Announcement
        {
            ClassRoomId = classId,
            AuthorId = CurrentUserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(announcement, "Đăng thông báo thành công."));
    }
}
