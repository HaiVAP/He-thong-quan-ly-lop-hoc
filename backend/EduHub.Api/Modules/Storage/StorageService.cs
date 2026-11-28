using System.Net;
using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EduHub.Contracts;

namespace EduHub.Api.Modules.Storage;

public class StorageService : IStorageService
{
    private readonly string _bucketName;
    private readonly AmazonS3Client _internalClient;
    private readonly AmazonS3Client _publicClient;
    private readonly bool _publicIsHttp;

    public StorageService(IConfiguration config)
    {
        var section = config.GetSection("Storage:Minio");
        var internalEndpoint = section["InternalEndpoint"] ?? "http://minio:9000";
        var publicEndpoint = section["PublicEndpoint"] ?? "http://localhost:9000";
        var accessKey = section["AccessKey"] ?? "eduhub_minio";
        var secretKey = section["SecretKey"] ?? "eduhub_minio_secret_123";
        _bucketName = section["BucketName"] ?? "eduhub-bucket";

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        _internalClient = CreateClient(credentials, internalEndpoint);
        _publicClient = CreateClient(credentials, publicEndpoint);
        _publicIsHttp = publicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    private static AmazonS3Client CreateClient(AWSCredentials credentials, string endpoint)
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };
        return new AmazonS3Client(credentials, s3Config);
    }

    public Task<PresignedUploadResponse> GeneratePresignedUploadUrlAsync(
        string originalFileName,
        string contentType,
        long sizeBytes)
    {
        var ext = Path.GetExtension(originalFileName);
        var randomId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var safeName = Path.GetFileNameWithoutExtension(originalFileName).Replace(" ", "_");
        var fileKey = $"{DateTime.UtcNow:yyyyMMdd}/{randomId}_{safeName}{ext}";

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = contentType,
            Protocol = _publicIsHttp ? Protocol.HTTP : Protocol.HTTPS
        };

        var uploadUrl = _publicClient.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUploadResponse(fileKey, uploadUrl, "PUT", expiresAt));
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string fileKey,
        string originalFileName,
        int expiryMinutes = 60,
        bool inline = false)
    {
        var dispositionType = inline ? "inline" : "attachment";
        var encodedName = Uri.EscapeDataString(originalFileName);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Protocol = _publicIsHttp ? Protocol.HTTP : Protocol.HTTPS,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"{dispositionType}; filename*=UTF-8''{encodedName}"
            }
        };

        var downloadUrl = _publicClient.GetPreSignedURL(request);
        return Task.FromResult(downloadUrl);
    }

    public async Task UploadStreamAsync(
        Stream stream,
        string fileKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            InputStream = stream,
            ContentType = contentType
        };

        await _internalClient.PutObjectAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _internalClient.GetObjectMetadataAsync(_bucketName, fileKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteFileAsync(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return;

        await _internalClient.DeleteObjectAsync(_bucketName, fileKey);
    }
}
