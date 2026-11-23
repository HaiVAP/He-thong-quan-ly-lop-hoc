using System.Security.Claims;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using EduHub.Api.Modules.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Materials;

[ApiController]
[Authorize]
public class MaterialsController(EduHubDbContext db, IStorageService storage) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsLecturer => User.IsInRole("Lecturer");

    private async Task<bool> CanAccessClassAsync(int classId) =>
        IsAdmin ||
        await db.Classes.AnyAsync(x => x.Id == classId && x.LecturerId == CurrentUserId) ||
        await db.ClassMembers.AnyAsync(x => x.ClassRoomId == classId && x.StudentId == CurrentUserId && x.Status == "Active");

    public record CreateCategoryRequest(string Name, string? Description, int SortOrder = 0);
    public record CreateMaterialRequest(
        string Title,
        string? Description,
        int? CategoryId,
        string FileName,
        string FileKey,
        string FileUrl,
        string? MimeType,
        long FileSizeBytes);

    // ==========================================
    // 1. QUẢN LÝ CHƯƠNG / CHỦ ĐỀ (CATEGORIES - FR-16)
    // ==========================================

    /// <summary>
    /// FR-16: Xem danh sách các chương / chủ đề tài liệu trong lớp
    /// </summary>
    [HttpGet("api/classes/{classId:int}/categories")]
    public async Task<IActionResult> GetCategories(int classId)
    {
        if (!await CanAccessClassAsync(classId))
            throw new ForbiddenException("Bạn không có quyền truy cập lớp học này.");

        var categories = await db.MaterialCategories
            .AsNoTracking()
            .Where(x => x.ClassRoomId == classId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ClassRoomId,
                c.Name,
                c.Description,
                c.SortOrder,
                c.CreatedAt,
                MaterialCount = c.Materials.Count
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(categories));
    }

    /// <summary>
    /// FR-16: Giảng viên tạo chương / chủ đề mới
    /// </summary>
    [HttpPost("api/classes/{classId:int}/categories")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> CreateCategory(int classId, [FromBody] CreateCategoryRequest request)
    {
        var classRoom = await db.Classes.FindAsync(classId);
        if (classRoom is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (classRoom.LecturerId != CurrentUserId)
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

        return Ok(ApiResponse.Ok(category, "Tạo chương mục thành công."));
    }

    /// <summary>
    /// Giảng viên xóa chương / chủ đề
    /// </summary>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.MaterialCategories.Include(x => x.ClassRoom).SingleOrDefaultAsync(x => x.Id == id);
        if (category is null)
            throw new NotFoundException($"Không tìm thấy chương mục có ID {id}.");

        if (!IsAdmin && category.ClassRoom.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn không có quyền xóa chương mục này.");

        db.MaterialCategories.Remove(category);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã xóa chương mục thành công."));
    }

    // ==========================================
    // 2. QUẢN LÝ TÀI LIỆU HỌC TẬP (MATERIALS - FR-15 & FR-17)
    // ==========================================

    /// <summary>
    /// FR-17: Sinh viên & Giảng viên xem danh sách tài liệu trong lớp học
    /// </summary>
    [HttpGet("api/classes/{classId:int}/materials")]
    public async Task<IActionResult> GetMaterials(int classId, [FromQuery] int? categoryId)
    {
        if (!await CanAccessClassAsync(classId))
            throw new ForbiddenException("Bạn không có quyền truy cập tài liệu của lớp học này.");

        var query = db.Materials
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.CreatedBy)
            .Where(x => x.ClassRoomId == classId);

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var materials = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.ClassRoomId,
                m.CategoryId,
                CategoryName = m.Category != null ? m.Category.Name : null,
                m.Title,
                m.Description,
                m.FileName,
                m.FileUrl,
                m.FileSizeBytes,
                m.MimeType,
                m.CreatedAt,
                Author = new { m.CreatedBy.Id, m.CreatedBy.FullName, m.CreatedBy.Email }
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(materials));
    }

    /// <summary>
    /// FR-15: Giảng viên đăng tài liệu học tập mới vào lớp
    /// </summary>
    [HttpPost("api/classes/{classId:int}/materials")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> CreateMaterial(int classId, [FromBody] CreateMaterialRequest request)
    {
        var classRoom = await db.Classes.FindAsync(classId);
        if (classRoom is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (classRoom.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể đăng tài liệu cho lớp học do mình phụ trách.");

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.FileUrl))
            throw new BadRequestException("Tiêu đề và Đường dẫn tệp tài liệu không được để trống.");

        if (request.CategoryId.HasValue && !await db.MaterialCategories.AnyAsync(x => x.Id == request.CategoryId.Value && x.ClassRoomId == classId))
        {
            throw new BadRequestException("Chương / chủ đề được chọn không tồn tại trong lớp học này.");
        }

        var material = new Material
        {
            ClassRoomId = classId,
            CategoryId = request.CategoryId,
            CreatedById = CurrentUserId,
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

        return Ok(ApiResponse.Ok(material, "Đăng tài liệu học tập thành công."));
    }

    /// <summary>
    /// FR-15: Giảng viên hoặc Admin xóa tài liệu học tập
    /// </summary>
    [HttpDelete("api/materials/{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        var material = await db.Materials.Include(x => x.ClassRoom).SingleOrDefaultAsync(x => x.Id == id);
        if (material is null)
            throw new NotFoundException($"Không tìm thấy tài liệu có ID {id}.");

        if (!IsAdmin && material.ClassRoom.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn không có quyền xóa tài liệu này.");

        // Xóa file vật lý trong storage nếu có
        if (!string.IsNullOrEmpty(material.FileKey))
        {
            await storage.DeleteFileAsync(material.FileKey);
        }

        db.Materials.Remove(material);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã xóa tài liệu học tập thành công."));
    }

    /// <summary>
    /// FR-17: Lấy liên kết tải tệp an toàn (Download Link)
    /// </summary>
    [HttpGet("api/materials/{id:int}/download")]
    public async Task<IActionResult> GetDownloadLink(int id)
    {
        var material = await db.Materials.FindAsync(id);
        if (material is null)
            throw new NotFoundException($"Không tìm thấy tài liệu có ID {id}.");

        if (!await CanAccessClassAsync(material.ClassRoomId))
            throw new ForbiddenException("Bạn không có quyền tải tài liệu của lớp học này.");

        var downloadUrl = await storage.GeneratePresignedDownloadUrlAsync(material.FileKey, material.FileName);

        return Ok(ApiResponse.Ok(new
        {
            material.Id,
            material.Title,
            material.FileName,
            downloadUrl
        }));
    }
}

