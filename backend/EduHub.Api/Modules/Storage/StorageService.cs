using System.Security.Cryptography;

namespace EduHub.Api.Modules.Storage;

public class StorageService : IStorageService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly string _localStoragePath;
    private readonly bool _useR2;

    public StorageService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
        _localStoragePath = Path.Combine(_env.ContentRootPath, _config["Storage:LocalStoragePath"] ?? "uploads");
        if (!Directory.Exists(_localStoragePath))
        {
            Directory.CreateDirectory(_localStoragePath);
        }

        var provider = _config["Storage:Provider"] ?? "Local";
        var r2Key = _config["Storage:R2:AccessKeyId"];
        _useR2 = provider.Equals("CloudflareR2", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(r2Key);
    }

    public Task<PresignedUploadResponse> GeneratePresignedUploadUrlAsync(string originalFileName, string contentType, long sizeBytes)
    {
        var ext = Path.GetExtension(originalFileName);
        var randomId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var safeName = Path.GetFileNameWithoutExtension(originalFileName).Replace(" ", "_");
        var fileKey = $"{DateTime.UtcNow:yyyyMMdd}/{randomId}_{safeName}{ext}";

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        if (_useR2)
        {
            // Nếu có cấu hình Cloudflare R2 (S3 API Endpoint)
            var accountId = _config["Storage:R2:AccountId"];
            var bucket = _config["Storage:R2:BucketName"];
            var publicUrl = _config["Storage:R2:PublicUrl"] ?? $"https://{bucket}.{accountId}.r2.cloudflarestorage.com";

            // Giả lập hoặc cấu hình endpoint R2
            var r2UploadUrl = $"{publicUrl}/{fileKey}?token={Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
            var downloadUrl = $"{publicUrl}/{fileKey}";

            return Task.FromResult(new PresignedUploadResponse(fileKey, r2UploadUrl, "PUT", expiresAt, downloadUrl));
        }

        // Chế độ Local Storage Fallback: upload qua endpoint của API Server
        var localUploadUrl = $"/api/files/upload/{fileKey}";
        var localDownloadUrl = $"/api/files/download/{fileKey}";

        return Task.FromResult(new PresignedUploadResponse(fileKey, localUploadUrl, "POST", expiresAt, localDownloadUrl));
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, string originalFileName, int expiryMinutes = 60)
    {
        if (_useR2)
        {
            var publicUrl = _config["Storage:R2:PublicUrl"] ?? "https://pub-r2.eduhub.local";
            return Task.FromResult($"{publicUrl}/{fileKey}");
        }

        return Task.FromResult($"/api/files/download/{fileKey}?filename={Uri.EscapeDataString(originalFileName)}");
    }

    public async Task<string> SaveLocalFileAsync(Stream stream, string fileKey, string originalFileName)
    {
        var filePath = Path.Combine(_localStoragePath, fileKey.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);

        return $"/api/files/download/{fileKey}?filename={Uri.EscapeDataString(originalFileName)}";
    }

    public Task<(Stream Stream, string ContentType, string FileName)?> GetLocalFileAsync(string fileKey)
    {
        var filePath = Path.Combine(_localStoragePath, fileKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream Stream, string ContentType, string FileName)?>(null);
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" or ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" or ".rar" => "application/zip",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(filePath);
        return Task.FromResult<(Stream Stream, string ContentType, string FileName)?>((stream, contentType, fileName));
    }

    public Task DeleteFileAsync(string fileKey)
    {
        var filePath = Path.Combine(_localStoragePath, fileKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}

