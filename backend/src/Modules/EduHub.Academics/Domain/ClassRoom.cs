namespace EduHub.Academics.Domain;

public class ClassRoom
{
    public int Id { get; set; }
    public int LecturerId { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JoinCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ClassMember> Members { get; set; } = [];
}

