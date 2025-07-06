using ImageHub.Application.Models.Storage;
using ImageHub.Domain.Common;

namespace ImageHub.Application.Abstractions;

public interface IImageStorageService
{
    Task<Result<string>> UploadAsync(ImageUploadRequest request, Stream content, CancellationToken ct = default);

    Task<Result<string>> GetPresignedUrlAsync(string key, CancellationToken ct = default);

    Task<Result> DeleteAsync(string key, CancellationToken ct = default);
    
    Task<Result> DeleteRecursivelyAsync(Guid imageId, CancellationToken ct = default);
    
    Task<Result<Stream>> GetAsync(string key, CancellationToken ct = default);
}