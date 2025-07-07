using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ImageHub.Domain.Common;
using ImageHub.Domain.Errors;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace ImageHub.Infrastructure.Tests.Repositories;

public class DynamoDbImageRepositoryTests
{
    private readonly DynamoDbImageRepository _sut;
    private readonly FakeLogger<DynamoDbImageRepository> _logger;
    private readonly IDynamoDBContext _dynamoDbContext;

    public DynamoDbImageRepositoryTests()
    {
        _logger = new FakeLogger<DynamoDbImageRepository>();
        _dynamoDbContext = Substitute.For<IDynamoDBContext>();
        _sut = new DynamoDbImageRepository(_logger, _dynamoDbContext);
    }

    [Fact]
    public async Task SaveImageAsync_ShouldReturnSuccess_WhenImageIsSavedSuccessfully()
    {
        // Arrange
        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, 1200);
        _dynamoDbContext.SaveAsync(Arg.Any<ImageDynamoDbModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SaveImageAsync(image);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldReturnFailure_WhenDynamoDbThrowsException()
    {
        // Arrange
        var image = Domain.Entities.Image.Create("test.jpg", "image/jpeg", 1024, 1200);
        _dynamoDbContext.SaveAsync(Arg.Any<ImageDynamoDbModel>(), Arg.Any<CancellationToken>())
            .Throws(new AmazonDynamoDBException("Save failed"));

        // Act
        var result = await _sut.SaveImageAsync(image);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ImageErrors.RecordSaveFailed);

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to save image '{image.Id}' to DynamoDB");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task GetImageAsync_ShouldReturnImage_WhenImageExists()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var dynamoDbModel = new ImageDynamoDbModel
        {
            Id = imageId,
            OriginalFileName = "test.jpg",
            MimeType = "image/jpeg",
            SizeInBytes = 1024,
            OriginalHeight = 1200,
            CreatedAt = DateTime.UtcNow,
            Sizes = new Dictionary<string, string>()
        };
        _dynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Returns(dynamoDbModel);

        // Act
        var result = await _sut.GetImageAsync(imageId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(imageId);
    }

    [Fact]
    public async Task GetImageAsync_ShouldReturnFailure_WhenImageDoesNotExist()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _dynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())!
            .Returns(null as ImageDynamoDbModel);

        // Act
        var result = await _sut.GetImageAsync(imageId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image record with Id '{imageId}' not found");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldBe($"Image '{imageId}' not found");
        logMessages[0].Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task GetImageAsync_ShouldReturnFailure_WhenDynamoDbThrowsException()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _dynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Throws(new AmazonDynamoDBException("Load failed"));

        // Act
        var result = await _sut.GetImageAsync(imageId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image record with Id '{imageId}' retrieval failed");
        
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to retrieve image '{imageId}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldReturnSuccess_WhenImageIsDeletedSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _dynamoDbContext.DeleteAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteImageAsync(imageId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldReturnFailure_WhenDynamoDbThrowsException()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _dynamoDbContext.DeleteAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Throws(new AmazonDynamoDBException("Delete failed"));

        // Act
        var result = await _sut.DeleteImageAsync(imageId);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<Error>();
        result.Error.Description.ShouldContain($"Image record with Id '{imageId}' deletion failed");
    
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to delete image '{imageId}'");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }
}