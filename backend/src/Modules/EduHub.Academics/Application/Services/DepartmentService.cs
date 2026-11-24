using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Domain;
using EduHub.Academics.Infrastructure;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Academics.Application.Services;

public class DepartmentService(AcademicsDbContext db) : IDepartmentService
{
    public async Task<List<Department>> GetAllAsync()
    {
        return await db.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Department> GetByIdAsync(int id)
    {
        var department = await db.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        return department;
    }

    public async Task<Department> CreateAsync(CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Tên khoa và Mã khoa không được để trống.");

        var code = request.Code.Trim().ToUpper();
        if (await db.Departments.AnyAsync(x => x.Code == code))
            throw new ConflictException($"Mã khoa '{code}' đã tồn tại trong hệ thống.");

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim()
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return department;
    }

    public async Task<Department> UpdateAsync(int id, UpdateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Tên khoa và Mã khoa không được để trống.");

        var department = await db.Departments.FindAsync(id);
        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        var code = request.Code.Trim().ToUpper();
        if (await db.Departments.AnyAsync(x => x.Code == code && x.Id != id))
            throw new ConflictException($"Mã khoa '{code}' đã được sử dụng bởi khoa khác.");

        department.Name = request.Name.Trim();
        department.Code = code;
        department.Description = request.Description?.Trim();

        await db.SaveChangesAsync();

        return department;
    }

    public async Task DeleteAsync(int id)
    {
        var department = await db.Departments.FindAsync(id);
        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        var hasClasses = await db.Classes.AnyAsync(x => x.DepartmentId == id);
        if (hasClasses)
            throw new BadRequestException("Không thể xóa khoa đang có lớp học trực thuộc.");

        db.Departments.Remove(department);
        await db.SaveChangesAsync();
    }
}

