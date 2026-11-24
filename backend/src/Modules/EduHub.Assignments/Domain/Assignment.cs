namespace EduHub.Assignments.Domain;

public class Assignment
{
    public int Id { get; set; }
    public int ClassRoomId { get; set; }
    public int CreatedById { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public bool AllowResubmit { get; set; } = true;
    public int MaxAttempts { get; set; } = 2;
    public string Status { get; set; } = "Published";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Submission> Submissions { get; set; } = [];
}

