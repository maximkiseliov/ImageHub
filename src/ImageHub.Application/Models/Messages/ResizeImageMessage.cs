namespace ImageHub.Application.Models.Messages;

public sealed class ResizeImageMessage
{
    public required Guid ImageId { get; init; }
    public required int TargetHeight { get; init; }
}