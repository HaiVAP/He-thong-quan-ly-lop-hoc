using EduHub.Contracts;
using EduHub.Shared.Exceptions;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Api.Modules.Storage;

[ApiController]
[Route("api/files")]
[Authorize]
public class StorageController(IStorageService storage) : ControllerBase
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".txt", ".csv", ".md", ".zip", ".rar", ".7z",
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".mp4", ".webm", ".mp3"
    };

    public record PresignedUrlRequest(string FileName, string ContentType, long SizeBytes);

    /// <summary>
    /// Xin cấp Presigned Upload URL (PUT, hạn 15 phút) để tải file trực tiếp lên MinIO
    /// </summary>
    [HttpPost("presigned-upload-url")]
    public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignedUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new BadRequestException("Tên tệp tin không được để trống.");

        if (request.SizeBytes <= 0)
            throw new BadRequestException("Kích thước tệp không hợp lệ.");

        if (request.SizeBytes > MaxFileSizeBytes)
            throw new BadRequestException("Kích thước tệp không được vượt quá 100MB.");

        var ext = Path.GetExtension(request.FileName.Trim());
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new BadRequestException($"Định dạng tệp '{ext}' không được phép tải lên.");

        var result = await storage.GeneratePresignedUploadUrlAsync(
            request.FileName.Trim(),
            string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType.Trim(),
            request.SizeBytes);

        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Xin cấp Presigned Download URL (GET, có thời hạn) cho tệp theo fileKey
    /// </summary>
    [HttpGet("presigned-download-url")]
    public async Task<IActionResult> GetPresignedDownloadUrl(
        [FromQuery] string fileKey,
        [FromQuery] string? fileName,
        [FromQuery] bool inline = false)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            throw new BadRequestException("Thiếu fileKey.");

        var name = string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(fileKey) : fileName.Trim();
        var downloadUrl = await storage.GeneratePresignedDownloadUrlAsync(fileKey.Trim(), name, 60, inline);

        return Ok(ApiResponse.Ok(new { fileKey, downloadUrl }));
    }
}
