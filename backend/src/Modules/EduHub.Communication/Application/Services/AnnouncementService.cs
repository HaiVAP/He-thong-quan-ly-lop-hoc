using EduHub.Communication.Application.DTOs;
using EduHub.Communication.Domain;
using EduHub.Communication.Infrastructure;
using EduHub.Contracts;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Communication.Application.Services;

public class AnnouncementService(
    CommunicationDbContext db,
    IClassAccess classAccess,
    IUserDirectory userDirectory) : IAnnouncementService
{
    public async Task<List<AnnouncementResponseDto>> ListByClassAsync(int classId, int currentUserId, bool isAdmin)
    {
        var classSummary = await classAccess.GetClassSummaryAsync(classId);
        if (classSummary is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        var canAccess = isAdmin || await classAccess.CanAccessClassAsync(classId, currentUserId);
        if (!canAccess)
            throw new ForbiddenException("Bạn không có quyền xem thông báo của lớp học này.");

        var announcements = await db.Announcements
            .AsNoTracking()
            .Where(x => x.ClassRoomId == classId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var authorIds = announcements.Select(x => x.AuthorId).Distinct().ToList();
        var authors = await userDirectory.GetByIdsAsync(authorIds);

        return announcements.Select(x => new AnnouncementResponseDto
        {
            Id = x.Id,
            Title = x.Title,
            Content = x.Content,
            CreatedAt = x.CreatedAt,
            Author = authors.TryGetValue(x.AuthorId, out var author) ? author : null
        }).ToList();
    }

    public async Task<Announcement> CreateAsync(int classId, int currentUserId, CreateAnnouncementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            throw new BadRequestException("Tiêu đề và Nội dung thông báo không được để trống.");

        var classSummary = await classAccess.GetClassSummaryAsync(classId);
        if (classSummary is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        var isLecturer = await classAccess.IsLecturerOfClassAsync(classId, currentUserId);
        if (!isLecturer)
            throw new ForbiddenException("Bạn chỉ có thể tạo thông báo cho lớp học do mình phụ trách.");

        var announcement = new Announcement
        {
            ClassRoomId = classId,
            AuthorId = currentUserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        return announcement;
    }
}

