using EduHub.Materials.Application.DTOs;
using EduHub.Materials.Domain;

namespace EduHub.Materials.Application.Services;

public interface IMaterialService
{
    Task<List<MaterialCategoryResponseDto>> GetCategoriesAsync(int classId, int currentUserId, bool isAdmin);
    Task<MaterialCategory> CreateCategoryAsync(int classId, int currentUserId, CreateCategoryRequest request);
    Task DeleteCategoryAsync(int id, int currentUserId, bool isAdmin);
    Task<List<MaterialResponseDto>> GetMaterialsAsync(int classId, int currentUserId, bool isAdmin, int? categoryId);
    Task<Material> CreateMaterialAsync(int classId, int currentUserId, CreateMaterialRequest request);
    Task DeleteMaterialAsync(int id, int currentUserId, bool isAdmin);
    Task<object> GetDownloadLinkAsync(int id, int currentUserId, bool isAdmin);
}
