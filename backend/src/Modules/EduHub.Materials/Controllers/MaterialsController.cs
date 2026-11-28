using System.Security.Claims;
using EduHub.Materials.Application.DTOs;
using EduHub.Materials.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Materials.Controllers;

[ApiController]
[Authorize]
public class MaterialsController(IMaterialService materialService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet("api/classes/{classId:int}/categories")]
    public async Task<IActionResult> GetCategories(int classId)
    {
        var categories = await materialService.GetCategoriesAsync(classId, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok(categories));
    }

    [HttpPost("api/classes/{classId:int}/categories")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> CreateCategory(int classId, [FromBody] CreateCategoryRequest request)
    {
        var category = await materialService.CreateCategoryAsync(classId, CurrentUserId, request);
        return Ok(ApiResponse.Ok(category, "Tạo chương mục thành công."));
    }

    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await materialService.DeleteCategoryAsync(id, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok("Đã xóa chương mục thành công."));
    }

    [HttpGet("api/classes/{classId:int}/materials")]
    public async Task<IActionResult> GetMaterials(int classId, [FromQuery] int? categoryId)
    {
        var materials = await materialService.GetMaterialsAsync(classId, CurrentUserId, IsAdmin, categoryId);
        return Ok(ApiResponse.Ok(materials));
    }

    [HttpPost("api/classes/{classId:int}/materials")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> CreateMaterial(int classId, [FromBody] CreateMaterialRequest request)
    {
        var material = await materialService.CreateMaterialAsync(classId, CurrentUserId, request);
        return Ok(ApiResponse.Ok(material, "Đăng tài liệu học tập thành công."));
    }

    [HttpDelete("api/materials/{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        await materialService.DeleteMaterialAsync(id, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok("Đã xóa tài liệu học tập thành công."));
    }

    [HttpGet("api/materials/{id:int}/download")]
    public async Task<IActionResult> GetDownloadLink(int id, [FromQuery] bool inline = false)
    {
        var result = await materialService.GetDownloadLinkAsync(id, CurrentUserId, IsAdmin, inline);
        return Ok(ApiResponse.Ok(result));
    }
}
