namespace EduHub.Materials.Application.DTOs;

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

public record MaterialCategoryResponseDto(
    int Id,
    int ClassRoomId,
    string Name,
    string? Description,
    int SortOrder,
    DateTime CreatedAt,
    int MaterialCount);

public record MaterialAuthorDto(int Id, string FullName, string Email);

public record MaterialResponseDto(
    int Id,
    int ClassRoomId,
    int? CategoryId,
    string? CategoryName,
    string Title,
    string? Description,
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    string? MimeType,
    DateTime CreatedAt,
    MaterialAuthorDto? Author);
