namespace ImageHub.Application.Models.Requests;

public sealed class ResizeImageRequest
{
    public required Guid Id { get; init; }
    public required int Height { get; init; }
}