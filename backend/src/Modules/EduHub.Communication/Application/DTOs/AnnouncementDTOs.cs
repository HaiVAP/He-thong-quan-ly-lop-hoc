using EduHub.Contracts;

namespace EduHub.Communication.Application.DTOs;

public record CreateAnnouncementRequest(string Title, string Content);

public class AnnouncementResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public UserSummaryDto? Author { get; set; }
}

