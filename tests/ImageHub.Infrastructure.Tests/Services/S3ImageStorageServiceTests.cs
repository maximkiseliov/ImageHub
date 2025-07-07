using Amazon.S3;
using Amazon.S3.Model;
using ImageHub.Application.Models.Storage;
using ImageHub.Domain.Common;
using ImageHub.Domain.Errors;
using ImageHub.Infrastructure.Services;
using ImageHub.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace ImageHub.Infrastructure.Tests.Services;

public class S3ImageStorageServiceTests
{
    private readonly S3ImageStorageService _sut;
    private readonly FakeLogger<S3ImageStorageService> _logger;
    private readonly IAmazonS3 _amazonS3;

    public S3ImageStorageServiceTests()
    {
        _logger = new FakeLogger<S3ImageStorageService>();
        _amazonS3 = Substitute.For<IAmazonS3>();

        var s3Settings = Substitute.For<IOptions<S3Settings>>();
        var settings = new S3Settings { BucketName = "test-bucket", PreSignedUrlExpirationTimeInMinutes = 15 };
        s3Settings.Value.Returns(settings);

        _sut = new S3ImageStorageService(_logger, _amazonS3, s3Settings);
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnSuccess_WhenS3UploadSucceeds()
    {
        // Arrange
        var request = new ImageUploadRequest
        {
            Key = "images/test-id/original.jpg",
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };
        using var content = new MemoryStream([1, 2, 3]);

        _amazonS3.PutObjectAsync(
                Arg.Is<PutObjectRequest>(r =>
                    r.BucketName == "test-bucket" &&
                    r.Key == request.Key &&
                    r.ContentType == request.ContentType),
                Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        // Act
        var result = await _sut.UploadAsync(request, content);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(request.Key);
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnFailure_WhenS3ReturnsNonOkStatus()
    {
        // Arrange
        var request = new ImageUploadRequest
        {
            Key = "images/test-id/original.jpg",
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };
        using var content = new MemoryStream([1, 2, 3]);

        _amazonS3.PutObjectAsync(
                Arg.Any<PutObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.InternalServerError });

        // Act
        var result = await _sut.UploadAsync(request, content);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ImageErrors.FileUploadFailed);
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to upload image '{request.Key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnFailure_WhenS3ThrowsException()
    {
        // Arrange
        var request = new ImageUploadRequest
        {
            Key = "images/test-id/original.jpg",
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };
        using var content = new MemoryStream([1, 2, 3]);

        _amazonS3.PutObjectAsync(
                Arg.Any<PutObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("Upload failed"));

        // Act
        var result = await _sut.UploadAsync(request, content);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ImageErrors.FileUploadFailed);
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to upload image '{request.Key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ShouldReturnSuccess_WhenUrlIsGenerated()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";
        const string expectedUrl = "https://test-bucket.s3.amazonaws.com/images/test-id/original.jpg";

        _amazonS3.GetPreSignedURLAsync(Arg.Is<GetPreSignedUrlRequest>(r =>
                r.BucketName == "test-bucket" &&
                r.Key == key &&
                r.Verb == HttpVerb.GET))
            .Returns(expectedUrl);

        // Act
        var result = await _sut.GetPresignedUrlAsync(key);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedUrl);
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ShouldReturnFailure_WhenUrlIsEmpty()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";
        
        _amazonS3.GetPreSignedURLAsync(Arg.Any<GetPreSignedUrlRequest>())
            .Returns(string.Empty);

        // Act
        var result = await _sut.GetPresignedUrlAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' pre signed url generation failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ShouldReturnFailure_WhenS3ThrowsException()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";
        _amazonS3.GetPreSignedURLAsync(Arg.Any<GetPreSignedUrlRequest>())
            .Throws(new AmazonS3Exception("URL generation failed"));

        // Act
        var result = await _sut.GetPresignedUrlAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' pre signed url generation failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to generate pre signed url for image '{key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenS3DeleteSucceeds()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";

        _amazonS3.DeleteObjectAsync(
                Arg.Is<DeleteObjectRequest>(r =>
                    r.BucketName == "test-bucket" &&
                    r.Key == key),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.NoContent });

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenS3ReturnsNonNoContentStatus()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";

        _amazonS3.DeleteObjectAsync(
                Arg.Any<DeleteObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.InternalServerError });

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' deletion failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to delete image '{key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenS3ThrowsException()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";

        _amazonS3.DeleteObjectAsync(
                Arg.Any<DeleteObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("Delete failed"));

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' deletion failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to delete image '{key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task DeleteRecursivelyAsync_ShouldReturnSuccess_WhenAllObjectsDeletedSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var prefix = $"images/{imageId}/";
        var keys = new[]
        {
            $"images/{imageId}/original.jpg",
            $"images/{imageId}/medium.jpg",
            $"images/{imageId}/thumbnail.jpg"
        };

        _amazonS3.ListObjectsV2Async(
                Arg.Is<ListObjectsV2Request>(r =>
                    r.BucketName == "test-bucket" &&
                    r.Prefix == prefix),
                Arg.Any<CancellationToken>())
            .Returns(new ListObjectsV2Response
            {
                S3Objects = keys.Select(k => new S3Object { Key = k }).ToList()
            });

        foreach (var key in keys)
        {
            _amazonS3.DeleteObjectAsync(
                    Arg.Is<DeleteObjectRequest>(r => r.Key == key),
                    Arg.Any<CancellationToken>())
                .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.NoContent });
        }

        // Act
        var result = await _sut.DeleteRecursivelyAsync(imageId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteRecursivelyAsync_ShouldReturnFailure_WhenAnyDeleteFails()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var prefix = $"images/{imageId}/";
        var keys = new[]
        {
            $"images/{imageId}/original.jpg",
            $"images/{imageId}/medium.jpg",
            $"images/{imageId}/thumbnail.jpg"
        };

        _amazonS3.ListObjectsV2Async(
                Arg.Any<ListObjectsV2Request>(),
                Arg.Any<CancellationToken>())
            .Returns(new ListObjectsV2Response
            {
                S3Objects = keys.Select(k => new S3Object { Key = k }).ToList()
            });

        _amazonS3.DeleteObjectAsync(
                Arg.Is<DeleteObjectRequest>(r => r.Key == keys[0]),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.NoContent });

        _amazonS3.DeleteObjectAsync(
                Arg.Is<DeleteObjectRequest>(r => r.Key == keys[1]),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.InternalServerError });

        _amazonS3.DeleteObjectAsync(
                Arg.Is<DeleteObjectRequest>(r => r.Key == keys[2]),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.NoContent });

        // Act
        var result = await _sut.DeleteRecursivelyAsync(imageId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[1].Message.ShouldContain($"Failed to delete images '{prefix}' recursively");
        logMessages[1].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task DeleteRecursivelyAsync_ShouldReturnFailure_WhenS3ThrowsException()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var prefix = $"images/{imageId}/";

        _amazonS3.ListObjectsV2Async(
                Arg.Any<ListObjectsV2Request>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("List objects failed"));

        // Act
        var result = await _sut.DeleteRecursivelyAsync(imageId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{prefix}' deletion failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to delete images '{prefix}' recursively");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnSuccess_WhenFileExists()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";
        using var responseStream = new MemoryStream([1, 2, 3]);

        _amazonS3.GetObjectAsync(
                Arg.Is<GetObjectRequest>(r =>
                    r.BucketName == "test-bucket" &&
                    r.Key == key),
                Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                ResponseStream = responseStream
            });

        // Act
        var result = await _sut.GetAsync(key);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(responseStream);
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnFailure_WhenS3ReturnsNonOkStatus()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";

        _amazonS3.GetObjectAsync(
                Arg.Any<GetObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.NotFound });

        // Act
        var result = await _sut.GetAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' retrieval failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to retrieve image '{key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnFailure_WhenS3ThrowsException()
    {
        // Arrange
        const string key = "images/test-id/original.jpg";

        _amazonS3.GetObjectAsync(
                Arg.Any<GetObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("Get object failed"));

        // Act
        var result = await _sut.GetAsync(key);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image file with key '{key}' retrieval failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to retrieve image '{key}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }
}