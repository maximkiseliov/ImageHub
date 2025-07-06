namespace ImageHub.Application.Models.Storage;

public sealed class ImageUploadRequest
{
    public required string Key { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}