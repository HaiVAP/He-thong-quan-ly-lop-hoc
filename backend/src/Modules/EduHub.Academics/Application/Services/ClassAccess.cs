using EduHub.Academics.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Academics.Application.Services;

public class ClassAccess(AcademicsDbContext db) : IClassAccess
{
    public async Task<bool> IsLecturerOfClassAsync(int classId, int userId, CancellationToken cancellationToken = default)
    {
        return await db.Classes.AnyAsync(x => x.Id == classId && x.LecturerId == userId, cancellationToken);
    }

    public async Task<bool> IsMemberOfClassAsync(int classId, int userId, CancellationToken cancellationToken = default)
    {
        return await db.ClassMembers.AnyAsync(x => x.ClassRoomId == classId && x.StudentId == userId && x.Status == "Active", cancellationToken);
    }

    public async Task<bool> CanAccessClassAsync(int classId, int userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;
        return await IsLecturerOfClassAsync(classId, userId, cancellationToken) ||
               await IsMemberOfClassAsync(classId, userId, cancellationToken);
    }

    public async Task<ClassSummaryDto?> GetClassSummaryAsync(int classId, CancellationToken cancellationToken = default)
    {
        var c = await db.Classes.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.Id == classId)
            .Select(x => new ClassSummaryDto(
                x.Id,
                x.Name,
                x.Description,
                x.DepartmentId,
                x.Department.Name,
                x.LecturerId,
                x.Status
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return c;
    }
}

