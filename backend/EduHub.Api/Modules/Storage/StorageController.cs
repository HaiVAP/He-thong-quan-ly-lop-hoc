using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Api.Modules.Storage;

[ApiController]
[Route("api/files")]
public class StorageController(IStorageService storage) : ControllerBase
{
    public record PresignedUrlRequest(string FileName, string ContentType, long SizeBytes);

    /// <summary>
    /// Xin cấp Presigned Upload URL (hạn 15 phút) để tải file trực tiếp lên R2 hoặc Local
    /// </summary>
    [HttpPost("presigned-upload-url")]
    [Authorize]
    public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignedUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new BadRequestException("Tên tệp tin không được để trống.");

        if (request.SizeBytes <= 0)
            throw new BadRequestException("Kích thước tệp không hợp lệ.");

        // Giới hạn kích thước tệp tối đa 100MB
        if (request.SizeBytes > 100 * 1024 * 1024)
            throw new BadRequestException("Kích thước tệp không được vượt quá 100MB.");

        var result = await storage.GeneratePresignedUploadUrlAsync(
            request.FileName.Trim(),
            request.ContentType?.Trim() ?? "application/octet-stream",
            request.SizeBytes);

        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Endpoint tải file lên trực tiếp (Dành cho chế độ Local Storage Fallback khi không có R2)
    /// </summary>
    [HttpPost("upload/{**fileKey}")]
    [Authorize]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadLocalFile(string fileKey, [FromQuery] string? fileName)
    {
        if (Request.Body is null)
            throw new BadRequestException("Không có dữ liệu tệp được gửi lên.");

        var originalName = fileName ?? Path.GetFileName(fileKey);
        var downloadUrl = await storage.SaveLocalFileAsync(Request.Body, fileKey, originalName);

        return Ok(ApiResponse.Ok(new
        {
            fileKey,
            downloadUrl,
            message = "Tải tệp lên thành công."
        }));
    }

    /// <summary>
    /// Endpoint tải tệp xuống từ máy chủ (Local Storage Fallback)
    /// </summary>
    [HttpGet("download/{**fileKey}")]
    public async Task<IActionResult> DownloadLocalFile(string fileKey, [FromQuery] string? filename)
    {
        var fileData = await storage.GetLocalFileAsync(fileKey);
        if (fileData is null)
            throw new NotFoundException("Tệp tin không tồn tại hoặc đã bị xóa.");

        var downloadName = filename ?? fileData.Value.FileName;
        return File(fileData.Value.Stream, fileData.Value.ContentType, downloadName);
    }
}

