using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Academics.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    /// <summary>
    /// FR-06: Xem danh sách các khoa
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await departmentService.GetAllAsync();
        return Ok(ApiResponse.Ok(departments));
    }

    /// <summary>
    /// FR-06: Xem thông tin chi tiết một khoa
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await departmentService.GetByIdAsync(id);
        return Ok(ApiResponse.Ok(department));
    }

    /// <summary>
    /// FR-05: Admin thêm mới khoa
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = await departmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, ApiResponse.Ok(department, "Thêm khoa mới thành công."));
    }

    /// <summary>
    /// FR-05: Admin cập nhật thông tin khoa
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await departmentService.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok(department, "Cập nhật thông tin khoa thành công."));
    }

    /// <summary>
    /// FR-05: Admin xóa khoa
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await departmentService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Đã xóa khoa thành công."));
    }
}

