namespace EduHub.Academics.Application.DTOs;

public record CreateClassRequest(string Name, string? Description, int DepartmentId);
public record UpdateClassRequest(string Name, string? Description, int DepartmentId, string? Status);
public record JoinClassRequest(string JoinCode);

public record LecturerSummary(int Id, string FullName, string Email);
public record DepartmentSummary(int Id, string Name, string Code);

public record ClassDetailDto(
    int Id,
    string Name,
    string? Description,
    string? JoinCode,
    string Status,
    DateTime CreatedAt,
    LecturerSummary? Lecturer,
    DepartmentSummary? Department,
    int MemberCount,
    int AssignmentCount
);

public record ClassMemberDto(
    int Id,
    int StudentId,
    string FullName,
    string Email,
    string? Phone,
    DateTime JoinedAt,
    string Status
);

