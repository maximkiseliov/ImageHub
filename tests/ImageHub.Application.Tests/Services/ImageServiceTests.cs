using ImageHub.Application.Abstractions;
using ImageHub.Application.Models.Messages;
using ImageHub.Application.Models.Requests;
using ImageHub.Application.Models.Storage;
using ImageHub.Application.Services;
using ImageHub.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;

namespace ImageHub.Application.Tests.Services;

public class ImageServiceTests
{
    private readonly ImageService _sut;
    private readonly FakeLogger<ImageService> _logger;
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageRepository _imageRepository;
    private readonly IImageQueueService _imageQueueService;

    public ImageServiceTests()
    {
        _logger = new FakeLogger<ImageService>();
        _imageStorageService = Substitute.For<IImageStorageService>();
        _imageRepository = Substitute.For<IImageRepository>();
        _imageQueueService = Substitute.For<IImageQueueService>();
        _sut = new ImageService(_logger, _imageStorageService, _imageRepository, _imageQueueService);
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldReturnSuccessResult_WhenAllOperationsSucceed()
    {
        // Arrange
        var sampleImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "sample.jpg");
        var sampleImageBytes = await File.ReadAllBytesAsync(sampleImagePath);
        using var memorySteam = new MemoryStream(sampleImageBytes);
        var request = new UploadImageRequest
        {
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Content = memorySteam
        };

        _imageStorageService
            .UploadAsync(Arg.Any<ImageUploadRequest>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("storage/path"));

        _imageRepository
            .SaveImageAsync(Arg.Any<Domain.Entities.Image>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _imageQueueService
            .EnqueueResizeMessageAsync(Arg.Any<ResizeImageMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _sut.ProcessImageAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _imageStorageService.Received(1)
            .UploadAsync(Arg.Any<ImageUploadRequest>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _imageRepository.Received(1)
            .SaveImageAsync(Arg.Any<Domain.Entities.Image>(), Arg.Any<CancellationToken>());
        await _imageQueueService.Received(1)
            .EnqueueResizeMessageAsync(Arg.Any<ResizeImageMessage>(), Arg.Any<CancellationToken>());

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain("Successfully processed image");
        logMessages[0].Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task GetImageAsync_ShouldReturnPresignedUrl_WhenImageExistsWithRequestedSize()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        const int height = 800;
        var request = new GetImageRequest { Id = imageId, Height = height };
        const string expectedUrl = "https://storage.example.com/images/test.jpg";
        const string storagePath = "storage/path";

        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, 1200);
        image.AddSize(height.ToString(), storagePath);

        _imageRepository
            .GetImageAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(image));

        _imageStorageService
            .GetPresignedUrlAsync(storagePath, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedUrl));

        // Act
        var result = await _sut.GetImageAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedUrl);
    }

    [Fact]
    public async Task ResizeImageAsync_ShouldEnqueueResizeMessage_WhenImageExists()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        const int height = 600;
        var request = new ResizeImageRequest { Id = imageId, Height = height };

        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, 1200);

        _imageRepository
            .GetImageAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(image));

        _imageQueueService
            .EnqueueResizeMessageAsync(Arg.Any<ResizeImageMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _sut.ResizeImageAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        await _imageQueueService.Received(1)
            .EnqueueResizeMessageAsync(Arg.Any<ResizeImageMessage>(), Arg.Any<CancellationToken>());

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldDeleteFromRepositoryAndStorage_WhenImageExists()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, 1200);

        _imageRepository
            .GetImageAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(image));

        _imageStorageService
            .DeleteRecursivelyAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _imageRepository
            .DeleteImageAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _sut.DeleteImageAsync(imageId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _imageStorageService.Received(1).DeleteRecursivelyAsync(imageId, Arg.Any<CancellationToken>());
        await _imageRepository.Received(1).DeleteImageAsync(imageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessImageResizeAsync_ShouldResizeAndSaveImage_WhenMessageIsValid()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        const int targetHeight = 600;
        const int originalHeight = 1200;
        const string originalPath = "storage/original/path";

        var message = new MessageWrapper<ResizeImageMessage>
        {
            MessageId = messageId,
            Body = new ResizeImageMessage
            {
                ImageId = imageId,
                TargetHeight = targetHeight
            }
        };

        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, originalHeight);
        image.AddSize(originalHeight.ToString(), originalPath);

        _imageRepository
            .GetImageAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(image));


        var sampleImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "sample.jpg");
        var sampleImageBytes = await File.ReadAllBytesAsync(sampleImagePath);
        using var memorySteam = new MemoryStream(sampleImageBytes);
        var imageStorageResult = Result.Success<Stream>(memorySteam);
        _imageStorageService
            .GetAsync(originalPath, Arg.Any<CancellationToken>())
            .Returns(imageStorageResult);

        _imageStorageService
            .UploadAsync(Arg.Any<ImageUploadRequest>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("storage/resized/path"));

        _imageRepository
            .SaveImageAsync(Arg.Any<Domain.Entities.Image>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _sut.ProcessImageResizeAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        await _imageRepository.Received(1).SaveImageAsync(
            Arg.Is<Domain.Entities.Image>(i => i.GetImagePath(targetHeight) != null),
            Arg.Any<CancellationToken>());

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldBe($"Processing message '{messageId}', image '{imageId}'");
        logMessages[0].Level.ShouldBe(LogLevel.Information);
        logMessages[1].Message.ShouldBe($"Message '{messageId}', image '{imageId}' processed successfully");
        logMessages[1].Level.ShouldBe(LogLevel.Information);
    }
}