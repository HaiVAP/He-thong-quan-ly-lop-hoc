using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Domain;

namespace EduHub.Academics.Application.Services;

public interface IClassService
{
    Task<object> GetMyClassesAsync(int currentUserId, bool isLecturer, bool isStudent, bool isAdmin);
    Task<ClassDetailDto> GetByIdAsync(int id, int currentUserId, bool isLecturer, bool isStudent, bool isAdmin);
    Task<ClassRoom> CreateAsync(int currentUserId, CreateClassRequest request);
    Task<ClassRoom> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateClassRequest request);
    Task DeleteAsync(int id, int currentUserId, bool isAdmin);
    Task<string> RegenerateCodeAsync(int id, int currentUserId);
    Task<(int Id, string Name)> JoinAsync(int currentUserId, JoinClassRequest request);
    Task LeaveAsync(int id, int currentUserId);
    Task<List<ClassMemberDto>> GetMembersAsync(int id, int currentUserId, bool isAdmin);
    Task RemoveMemberAsync(int id, int studentId, int currentUserId, bool isAdmin);
}

