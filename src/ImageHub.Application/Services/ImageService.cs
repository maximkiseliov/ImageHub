using ImageHub.Application.Abstractions;
using ImageHub.Application.Helpers;
using ImageHub.Application.Models.Mappers;
using ImageHub.Application.Models.Messages;
using ImageHub.Application.Models.Requests;
using ImageHub.Domain.Common;
using ImageHub.Domain.Entities;
using ImageHub.Domain.Errors;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;
using ImageSharp = SixLabors.ImageSharp.Image;

namespace ImageHub.Application.Services;

public sealed class ImageService : IImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageRepository _imageRepository;
    private readonly IImageQueueService _imageQueueService;

    public ImageService(
        ILogger<ImageService> logger,
        IImageStorageService imageStorageService,
        IImageRepository imageRepository,
        IImageQueueService imageQueueService)
    {
        _logger = logger;
        _imageStorageService = imageStorageService;
        _imageRepository = imageRepository;
        _imageQueueService = imageQueueService;
    }

    public async Task<Result<Guid>> ProcessImageAsync(UploadImageRequest request, CancellationToken ct = default)
    {
        request.Content.Seek(0, SeekOrigin.Begin);
        using var imageSharp = await ImageSharp.LoadAsync(request.Content, ct);
        var image = Image.Create(request.FileName, request.ContentType, request.Content.Length, imageSharp.Height);

        var uploadRequest = image.Map(image.OriginalHeight);
        var fileUploadResult = await _imageStorageService.UploadAsync(uploadRequest, request.Content, ct);
        await request.Content.DisposeAsync();
        if (fileUploadResult.IsFailure)
        {
            return Result.Failure<Guid>(fileUploadResult.Error);
        }

        image.AddSize(image.OriginalHeight.ToString(), fileUploadResult.Value);
        var recordSaveResult = await _imageRepository.SaveImageAsync(image, ct);
        if (recordSaveResult.IsFailure)
        {
            return Result.Failure<Guid>(recordSaveResult.Error);
        }

        var resizeMessage = ModelMapper.Map(image.Id, PredefinedSize.Thumbnail);
        var resizeQueueResult = await _imageQueueService.EnqueueResizeMessageAsync(resizeMessage, ct);
        if (resizeQueueResult.IsFailure)
        {
            return Result.Failure<Guid>(resizeQueueResult.Error);
        }

        _logger.LogInformation("Successfully processed image '{ImageId}' with name '{FileName}", image.Id,
            request.FileName);
        return Result.Success(image.Id);
    }

    // TODO: Think about caching
    public async Task<Result<string>> GetImageAsync(GetImageRequest request, CancellationToken ct = default)
    {
        var imageRecordResult = await _imageRepository.GetImageAsync(request.Id, ct);
        if (imageRecordResult.IsFailure)
        {
            return Result.Failure<string>(imageRecordResult.Error);
        }

        var imageRequestedPath = imageRecordResult.Value.GetImagePath(request.Height);
        if (string.IsNullOrWhiteSpace(imageRequestedPath))
        {
            return Result.Failure<string>(ImageErrors.FileNotFound(request.Id, request.Height));
        }

        var preSignedUrl =
            await _imageStorageService.GetPresignedUrlAsync(imageRequestedPath, ct);

        return preSignedUrl.IsSuccess ? Result.Success(preSignedUrl.Value) : Result.Failure<string>(preSignedUrl.Error);
    }

    public async Task<Result> ResizeImageAsync(ResizeImageRequest request, CancellationToken ct = default)
    {
        var imageRecordResult = await _imageRepository.GetImageAsync(request.Id, ct);
        if (imageRecordResult.IsFailure)
        {
            return Result.Failure(imageRecordResult.Error);
        }

        if (request.Height <= 0 || request.Height > imageRecordResult.Value.OriginalHeight)
        {
            return Result.Failure(ImageErrors.InvalidHeight(request.Height));
        }

        if (!string.IsNullOrWhiteSpace(imageRecordResult.Value.GetImagePath(request.Height)))
        {
            _logger.LogInformation("Image '{ImageId}' is already exists in height '{Height}' px",
                imageRecordResult.Value.Id, request.Height);
            return Result.Success();
        }

        var resizeMessage = ModelMapper.Map(imageRecordResult.Value.Id, request.Height);
        var resizeQueueResult = await _imageQueueService.EnqueueResizeMessageAsync(resizeMessage, ct);

        return resizeQueueResult.IsSuccess ? Result.Success() : Result.Failure(resizeQueueResult.Error);
    }

    public async Task<Result> DeleteImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var getRecordResult = await _imageRepository.GetImageAsync(imageId, ct);
        if (getRecordResult.IsFailure)
        {
            return Result.Failure(getRecordResult.Error);
        }

        var fileDeletionResult = await _imageStorageService.DeleteRecursivelyAsync(imageId, ct);
        if (fileDeletionResult.IsFailure)
        {
            return Result.Failure(fileDeletionResult.Error);
        }

        var recordDeleteResult = await _imageRepository.DeleteImageAsync(imageId, ct);
        return recordDeleteResult.IsSuccess ? Result.Success() : Result.Failure(recordDeleteResult.Error);
    }

    public async Task<Result> ProcessImageResizeAsync(MessageWrapper<ResizeImageMessage> message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message.Body);

        _logger.LogInformation("Processing message '{MessageId}', image '{ImageId}'", message.MessageId,
            message.Body.ImageId);

        var recordGetResult = await _imageRepository.GetImageAsync(message.Body.ImageId, ct);
        if (recordGetResult.IsFailure)
        {
            return Result.Failure<string>(recordGetResult.Error);
        }

        var originalImagePath = recordGetResult.Value.GetImagePath(recordGetResult.Value.OriginalHeight);
        if (string.IsNullOrWhiteSpace(originalImagePath))
        {
            return Result.Failure<string>(ImageErrors.FileNotFound(recordGetResult.Value.Id,
                recordGetResult.Value.OriginalHeight));
        }

        var fileGetResult = await _imageStorageService.GetAsync(originalImagePath, ct);
        if (fileGetResult.IsFailure)
        {
            return Result.Failure<string>(fileGetResult.Error);
        }

        var resizedImageStream = await ResizeImageAsync(fileGetResult.Value, message.Body.TargetHeight,
            recordGetResult.Value.MimeType, ct);

        var uploadRequest = recordGetResult.Value.Map(message.Body.TargetHeight);
        var fileUploadResult = await _imageStorageService.UploadAsync(uploadRequest, resizedImageStream, ct);
        await resizedImageStream.DisposeAsync();
        if (fileUploadResult.IsFailure)
        {
            return Result.Failure(fileUploadResult.Error);
        }
        
        recordGetResult.Value.AddSize(message.Body.TargetHeight.ToString(), fileUploadResult.Value);
        var recordSaveResult = await _imageRepository.SaveImageAsync(recordGetResult.Value, ct);
        if (recordSaveResult.IsFailure)
        {
            return Result.Failure(recordSaveResult.Error);
        }

        _logger.LogInformation("Message '{MessageId}', image '{ImageId}' processed successfully",
            message.MessageId, message.Body.ImageId);

        return Result.Success();
    }

    private static async Task<Stream> ResizeImageAsync(Stream fileStorageStream, int targetHeight, string mimeType,
        CancellationToken ct = default)
    {
        using var inputStream = new MemoryStream();
        await fileStorageStream.CopyToAsync(inputStream, ct);
        await fileStorageStream.DisposeAsync();
        inputStream.Seek(0, SeekOrigin.Begin);
        
        using var image = await ImageSharp.LoadAsync(inputStream, ct);

        var ratio = (double)targetHeight / image.Height;
        var targetWidth = (int)(image.Width * ratio);

        image.Mutate(op => op.Resize(targetWidth, targetHeight));

        var outputStream = new MemoryStream();
        await image.SaveAsync(outputStream, EncoderHelper.GetEncoder(mimeType), ct);

        return outputStream;
    }
}