namespace EduHub.Api.Modules.Storage;

public record PresignedUploadResponse(string FileKey, string UploadUrl, string Method, DateTime ExpiresAt, string DownloadUrl);

public interface IStorageService
{
    /// <summary>
    /// Ký URL tải lên trực tiếp (Presigned Upload URL) với hạn dùng 15 phút
    /// </summary>
    Task<PresignedUploadResponse> GeneratePresignedUploadUrlAsync(string originalFileName, string contentType, long sizeBytes);

    /// <summary>
    /// Ký URL tải xuống an toàn (Presigned Download URL) có thời hạn
    /// </summary>
    Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, string originalFileName, int expiryMinutes = 60);

    /// <summary>
    /// Lưu tệp vào bộ nhớ cục bộ (cho chế độ Local Development / Offline)
    /// </summary>
    Task<string> SaveLocalFileAsync(Stream stream, string fileKey, string originalFileName);

    /// <summary>
    /// Đọc stream tệp từ bộ nhớ cục bộ
    /// </summary>
    Task<(Stream Stream, string ContentType, string FileName)?> GetLocalFileAsync(string fileKey);

    /// <summary>
    /// Xóa tệp
    /// </summary>
    Task DeleteFileAsync(string fileKey);
}

