using EduHub.Shared.Models;

namespace EduHub.Materials.Domain;

public class MaterialCategory : BaseEntity
{
    public int ClassRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public List<Material> Materials { get; set; } = [];
}
