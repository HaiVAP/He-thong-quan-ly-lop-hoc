using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Domain;

namespace EduHub.Academics.Application.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync();
    Task<Department> GetByIdAsync(int id);
    Task<Department> CreateAsync(CreateDepartmentRequest request);
    Task<Department> UpdateAsync(int id, UpdateDepartmentRequest request);
    Task DeleteAsync(int id);
}

