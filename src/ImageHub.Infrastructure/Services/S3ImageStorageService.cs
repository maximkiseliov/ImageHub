using Amazon.S3;
using Amazon.S3.Model;
using ImageHub.Application.Abstractions;
using ImageHub.Application.Models.Storage;
using ImageHub.Domain.Common;
using ImageHub.Domain.Errors;
using ImageHub.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageHub.Infrastructure.Services;

public sealed class S3ImageStorageService : IImageStorageService
{
    private readonly ILogger<S3ImageStorageService> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;

    public S3ImageStorageService(
        ILogger<S3ImageStorageService> logger,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        _logger = logger;
        _s3Client = s3Client;
        _s3Settings = s3Settings.Value;
    }

    public async Task<Result<string>> UploadAsync(ImageUploadRequest request, Stream content,
        CancellationToken ct = default)
    {
        content.Seek(0, SeekOrigin.Begin);

        var putRequest = new PutObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = request.Key,
            InputStream = content,
            ContentType = request.ContentType,
            Metadata =
            {
                ["x-amz-meta-originalname"] = request.FileName,
                ["x-amz-meta-extension"] = Path.GetExtension(request.FileName)
            }
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(putRequest, ct);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return Result.Success(request.Key);
            }

            _logger.LogError("Failed to upload image '{Key}'. StatusCode: {StatusCode}", request.Key,
                response.HttpStatusCode);
            return Result.Failure<string>(ImageErrors.FileUploadFailed);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image '{Key}'", request.Key);
            return Result.Failure<string>(ImageErrors.FileUploadFailed);
        }
    }

    public async Task<Result<string>> GetPresignedUrlAsync(string key, CancellationToken ct = default)
    {
        var preSignedUrlRequest = new GetPreSignedUrlRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PreSignedUrlExpirationTimeInMinutes)
        };

        try
        {
            var response = await _s3Client.GetPreSignedURLAsync(preSignedUrlRequest);
            return string.IsNullOrWhiteSpace(response)
                ? Result.Failure<string>(ImageErrors.PreSignedUrlGenerationFailed(key))
                : Result.Success(response);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre signed url for image '{Key}'", key);
            return Result.Failure<string>(ImageErrors.PreSignedUrlGenerationFailed(key));
        }
    }

    public async Task<Result> DeleteAsync(string key, CancellationToken ct = default)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = key
        };

        try
        {
            var response = await _s3Client.DeleteObjectAsync(deleteRequest, ct);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Result.Success();
            }

            _logger.LogError("Failed to delete image '{Key}'", key);
            return Result.Failure(ImageErrors.FileDeletionFailed(key));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image '{Key}'", key);
            return Result.Failure(ImageErrors.FileDeletionFailed(key));
        }
    }

    public async Task<Result> DeleteRecursivelyAsync(Guid imageId, CancellationToken ct = default)
    {
        var key = $"images/{imageId}/";
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _s3Settings.BucketName,
            Prefix = key
        };

        try
        {
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest, ct);

            List<Task<Result>> deleteTasks = [];
            deleteTasks.AddRange(listResponse.S3Objects.Select(s3Object => DeleteAsync(s3Object.Key, ct)));
            var deleteResults = await Task.WhenAll(deleteTasks);

            if (deleteResults.All(r => r.IsSuccess))
            {
                return Result.Success();
            }

            var errors = deleteResults.Where(r => r.IsFailure).Select(r => r.Error).ToArray();
            _logger.LogError("Failed to delete images '{Key}' recursively", key);
            return Result.Failure(new ValidationError(errors));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to delete images '{Key}'", key);
            return Result.Failure(ImageErrors.FileDeletionFailed(key));
        }
    }

    public async Task<Result<Stream>> GetAsync(string key, CancellationToken ct = default)
    {
        var getRequest = new GetObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = key
        };

        try
        {
            var response = await _s3Client.GetObjectAsync(getRequest, ct);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return Result.Success(response.ResponseStream);
            }

            _logger.LogError("Failed to retrieve image '{Key}'", key);
            return Result.Failure<Stream>(ImageErrors.FileRetrievalFailed(key));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve image '{Key}'", key);
            return Result.Failure<Stream>(ImageErrors.FileRetrievalFailed(key));
        }
    }
}