using EduHub.Shared.Models;
using EduHub.Academics.Domain;

namespace EduHub.Api.Modules.Materials;

public class MaterialCategory : BaseEntity
{
    public int ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public List<Material> Materials { get; set; } = [];
}

 