using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ImageHub.Application.Abstractions;
using ImageHub.Domain.Common;
using ImageHub.Domain.Entities;
using ImageHub.Domain.Errors;
using ImageHub.Infrastructure.Models.DynamoDb;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Repositories;

public sealed class DynamoDbImageRepository : IImageRepository
{
    private readonly ILogger<DynamoDbImageRepository> _logger;
    private readonly IDynamoDBContext _dynamoDbContext;

    public DynamoDbImageRepository(
        ILogger<DynamoDbImageRepository> logger,
        IDynamoDBContext dynamoDbContext)
    {
        _logger = logger;
        _dynamoDbContext = dynamoDbContext;
    }

    public async Task<Result> SaveImageAsync(Image image, CancellationToken ct = default)
    {
        var dynamoDbModel = Map(image);

        try
        {
            await _dynamoDbContext.SaveAsync(dynamoDbModel, ct);
            return Result.Success();
        }
        catch (AmazonDynamoDBException ex)
        {
            _logger.LogError(ex, "Failed to save image '{ImageId}' to DynamoDB", image.Id);
            return Result.Failure(ImageErrors.RecordSaveFailed);
        }
    }

    public async Task<Result<Image>> GetImageAsync(Guid imageId, CancellationToken ct = default)
    {
        try
        {
            var result = await _dynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, ct);
            if (result is null)
            {
                _logger.LogInformation("Image '{ImageId}' not found", imageId);
                return Result.Failure<Image>(ImageErrors.RecordNotFound(imageId));
            }

            var image = Image.FromPersistence(result.Id, result.OriginalFileName, result.MimeType, result.SizeInBytes,
                result.OriginalHeight, result.CreatedAt, result.Sizes);

            return Result.Success(image);
        }
        catch (AmazonDynamoDBException ex)
        {
            _logger.LogError(ex, "Failed to retrieve image '{ImageId}'", imageId);
            return Result.Failure<Image>(ImageErrors.RecordRetrievalFailed(imageId));
        }
    }

    public async Task<Result> DeleteImageAsync(Guid imageId, CancellationToken ct = default)
    {
        try
        {
            await _dynamoDbContext.DeleteAsync<ImageDynamoDbModel>(imageId, ct);
            return Result.Success();
        }
        catch (AmazonDynamoDBException ex)
        {
            _logger.LogError(ex, "Failed to delete image '{ImageId}'", imageId);
            return Result.Failure<Image>(ImageErrors.RecordDeletionFailed(imageId));
        }
    }

    // Move to mapper
    private static ImageDynamoDbModel Map(Image image)
    {
        return new ImageDynamoDbModel
        {
            Id = image.Id,
            OriginalFileName = image.OriginalFileName,
            MimeType = image.MimeType,
            SizeInBytes = image.SizeInBytes,
            OriginalHeight = image.OriginalHeight,
            CreatedAt = image.CreatedAt,
            Sizes = image.Sizes.ToDictionary()
        };
    }
}