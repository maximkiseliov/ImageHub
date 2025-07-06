using ImageHub.Domain.Common;
using ImageHub.Domain.Entities;

namespace ImageHub.Application.Abstractions;

public interface IImageRepository
{
    Task<Result> SaveImageAsync(Image image, CancellationToken ct = default);
    
    Task<Result<Image>> GetImageAsync(Guid imageId, CancellationToken ct = default);
    
    Task<Result> DeleteImageAsync(Guid imageId, CancellationToken ct = default);
}