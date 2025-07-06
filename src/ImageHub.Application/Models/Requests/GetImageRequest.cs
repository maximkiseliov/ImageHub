namespace ImageHub.Application.Models.Requests;

public sealed class GetImageRequest
{
    public required Guid Id { get; init; }
    public required int Height { get; init; }
}