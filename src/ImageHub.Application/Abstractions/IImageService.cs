using ImageHub.Application.Models.Requests;
using ImageHub.Application.Models.Messages;
using ImageHub.Domain.Common;

namespace ImageHub.Application.Abstractions;

public interface IImageService
{
    Task<Result<Guid>> ProcessImageAsync(UploadImageRequest request, CancellationToken ct = default);

    Task<Result<string>> GetImageAsync(GetImageRequest request, CancellationToken ct = default);

    Task<Result> ResizeImageAsync(ResizeImageRequest request, CancellationToken ct = default);

    Task<Result> DeleteImageAsync(Guid imageId, CancellationToken ct = default);

    Task<Result> ProcessImageResizeAsync(MessageWrapper<ResizeImageMessage> message, CancellationToken ct = default);
}