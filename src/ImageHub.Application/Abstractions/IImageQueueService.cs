using ImageHub.Application.Models.Messages;
using ImageHub.Domain.Common;

namespace ImageHub.Application.Abstractions;

public interface IImageQueueService
{
    Task<Result> EnqueueResizeMessageAsync(ResizeImageMessage message, CancellationToken ct = default);
}