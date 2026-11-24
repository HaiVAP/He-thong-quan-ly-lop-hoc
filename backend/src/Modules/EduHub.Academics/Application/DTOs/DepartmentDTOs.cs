namespace EduHub.Academics.Application.DTOs;

public record CreateDepartmentRequest(string Name, string Code, string? Description);
public record UpdateDepartmentRequest(string Name, string Code, string? Description);

