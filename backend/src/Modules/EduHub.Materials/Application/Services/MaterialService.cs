using EduHub.Contracts;
using EduHub.Contracts.Events;
using EduHub.Materials.Application.DTOs;
using EduHub.Materials.Domain;
using EduHub.Materials.Infrastructure;
using EduHub.Shared.Events;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Materials.Application.Services;

public class MaterialService(
    MaterialsDbContext db,
    IClassAccess classAccess,
    IUserDirectory userDirectory,
    IStorageService storage,
    IEventBus eventBus) : IMaterialService
{
    public async Task<List<MaterialCategoryResponseDto>> GetCategoriesAsync(int classId, int currentUserId, bool isAdmin)
    {
        if (!await classAccess.CanAccessClassAsync(classId, currentUserId, isAdmin))
            throw new ForbiddenException("Bạn không có quyền truy cập lớp học này.");

        return await db.MaterialCategories
            .AsNoTracking()
            .Where(x => x.ClassRoomId == classId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Select(category => new MaterialCategoryResponseDto(
                category.Id,
                category.ClassRoomId,
                category.Name,
                category.Description,
                category.SortOrder,
                category.CreatedAt,
                category.Materials.Count))
            .ToListAsync();
    }

    public async Task<MaterialCategory> CreateCategoryAsync(int classId, int currentUserId, CreateCategoryRequest request)
    {
        if (await classAccess.GetClassSummaryAsync(classId) is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (!await classAccess.IsLecturerOfClassAsync(classId, currentUserId))
            throw new ForbiddenException("Bạn chỉ có thể tạo chương mục cho lớp học do mình phụ trách.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Tên chương / chủ đề không được để trống.");

        var category = new MaterialCategory
        {
            ClassRoomId = classId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            SortOrder = request.SortOrder
        };

        db.MaterialCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task DeleteCategoryAsync(int id, int currentUserId, bool isAdmin)
    {
        var category = await db.MaterialCategories.SingleOrDefaultAsync(x => x.Id == id);
        if (category is null)
            throw new NotFoundException($"Không tìm thấy chương mục có ID {id}.");

        if (!isAdmin && !await classAccess.IsLecturerOfClassAsync(category.ClassRoomId, currentUserId))
            throw new ForbiddenException("Bạn không có quyền xóa chương mục này.");

        db.MaterialCategories.Remove(category);
        await db.SaveChangesAsync();
    }

    public async Task<List<MaterialResponseDto>> GetMaterialsAsync(
        int classId,
        int currentUserId,
        bool isAdmin,
        int? categoryId)
    {
        if (!await classAccess.CanAccessClassAsync(classId, currentUserId, isAdmin))
            throw new ForbiddenException("Bạn không có quyền truy cập tài liệu của lớp học này.");

        var query = db.Materials
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.ClassRoomId == classId);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        var materials = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var authors = await userDirectory.GetByIdsAsync(materials.Select(x => x.CreatedById).Distinct());

        return materials.Select(material =>
        {
            authors.TryGetValue(material.CreatedById, out var author);
            return new MaterialResponseDto(
                material.Id,
                material.ClassRoomId,
                material.CategoryId,
                material.Category?.Name,
                material.Title,
                material.Description,
                material.FileName,
                material.FileUrl,
                material.FileSizeBytes,
                material.MimeType,
                material.CreatedAt,
                author is null ? null : new MaterialAuthorDto(author.Id, author.FullName, author.Email));
        }).ToList();
    }

    public async Task<Material> CreateMaterialAsync(int classId, int currentUserId, CreateMaterialRequest request)
    {
        if (await classAccess.GetClassSummaryAsync(classId) is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (!await classAccess.IsLecturerOfClassAsync(classId, currentUserId))
            throw new ForbiddenException("Bạn chỉ có thể đăng tài liệu cho lớp học do mình phụ trách.");

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.FileUrl))
            throw new BadRequestException("Tiêu đề và Đường dẫn tệp tài liệu không được để trống.");

        if (request.CategoryId.HasValue &&
            !await db.MaterialCategories.AnyAsync(x => x.Id == request.CategoryId.Value && x.ClassRoomId == classId))
        {
            throw new BadRequestException("Chương / chủ đề được chọn không tồn tại trong lớp học này.");
        }

        var material = new Material
        {
            ClassRoomId = classId,
            CategoryId = request.CategoryId,
            CreatedById = currentUserId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            FileName = request.FileName.Trim(),
            FileKey = request.FileKey.Trim(),
            FileUrl = request.FileUrl.Trim(),
            MimeType = request.MimeType?.Trim(),
            FileSizeBytes = request.FileSizeBytes
        };

        db.Materials.Add(material);
        await db.SaveChangesAsync();

        await eventBus.PublishAsync(new MaterialUploadedEvent(
            material.Id,
            material.ClassRoomId,
            material.CreatedById,
            material.Title,
            material.FileName,
            material.CreatedAt));

        return material;
    }

    public async Task DeleteMaterialAsync(int id, int currentUserId, bool isAdmin)
    {
        var material = await db.Materials.SingleOrDefaultAsync(x => x.Id == id);
        if (material is null)
            throw new NotFoundException($"Không tìm thấy tài liệu có ID {id}.");

        if (!isAdmin && !await classAccess.IsLecturerOfClassAsync(material.ClassRoomId, currentUserId))
            throw new ForbiddenException("Bạn không có quyền xóa tài liệu này.");

        if (!string.IsNullOrEmpty(material.FileKey))
            await storage.DeleteFileAsync(material.FileKey);

        db.Materials.Remove(material);
        await db.SaveChangesAsync();
    }

    public async Task<object> GetDownloadLinkAsync(int id, int currentUserId, bool isAdmin)
    {
        var material = await db.Materials.FindAsync(id);
        if (material is null)
            throw new NotFoundException($"Không tìm thấy tài liệu có ID {id}.");

        if (!await classAccess.CanAccessClassAsync(material.ClassRoomId, currentUserId, isAdmin))
            throw new ForbiddenException("Bạn không có quyền tải tài liệu của lớp học này.");

        var downloadUrl = await storage.GeneratePresignedDownloadUrlAsync(material.FileKey, material.FileName);
        return new
        {
            material.Id,
            material.Title,
            material.FileName,
            downloadUrl
        };
    }
}
