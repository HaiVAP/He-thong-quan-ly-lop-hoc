using EduHub.Api.Common.Models;
using EduHub.Api.Modules.Academics;
using EduHub.Api.Modules.Identity;

namespace EduHub.Api.Modules.Materials;

public class Material : BaseEntity
{
    public int ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public int? CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
}

