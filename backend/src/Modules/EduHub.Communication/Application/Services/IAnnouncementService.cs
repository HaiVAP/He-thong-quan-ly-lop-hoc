using EduHub.Communication.Application.DTOs;
using EduHub.Communication.Domain;

namespace EduHub.Communication.Application.Services;

public interface IAnnouncementService
{
    Task<List<AnnouncementResponseDto>> ListByClassAsync(int classId, int currentUserId, bool isAdmin);
    Task<Announcement> CreateAsync(int classId, int currentUserId, CreateAnnouncementRequest request);
}

