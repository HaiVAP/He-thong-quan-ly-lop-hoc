using EduHub.Shared.Models;

namespace EduHub.Materials.Domain;

public class Material : BaseEntity
{
    public int ClassRoomId { get; set; }
    public int? CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }
    public int CreatedById { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
}
