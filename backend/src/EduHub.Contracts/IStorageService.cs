namespace EduHub.Contracts;

public record PresignedUploadResponse(string FileKey, string UploadUrl, string Method, DateTime ExpiresAt);

public interface IStorageService
{
    /// <summary>
    /// Ký URL tải lên trực tiếp (Presigned Upload URL - PUT, hạn 15 phút)
    /// </summary>
    Task<PresignedUploadResponse> GeneratePresignedUploadUrlAsync(string originalFileName, string contentType, long sizeBytes);

    /// <summary>
    /// Ký URL tải xuống / xem trực tiếp (Presigned Download URL - GET) có thời hạn
    /// </summary>
    Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, string originalFileName, int expiryMinutes = 60, bool inline = false);

    /// <summary>
    /// Upload trực tiếp stream lên MinIO từ backend
    /// </summary>
    Task UploadStreamAsync(Stream stream, string fileKey, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tệp có tồn tại trên MinIO hay không
    /// </summary>
    Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tệp trên MinIO
    /// </summary>
    Task DeleteFileAsync(string fileKey);
}
