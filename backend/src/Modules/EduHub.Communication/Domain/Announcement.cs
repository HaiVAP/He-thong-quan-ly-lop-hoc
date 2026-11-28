namespace EduHub.Communication.Domain;

public class Announcement
{
    public int Id { get; set; }
    public int ClassRoomId { get; set; }
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

