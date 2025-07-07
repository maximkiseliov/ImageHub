namespace ImageHub.Application.Models.Requests;

public sealed class UploadImageRequest
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}