namespace ImageHub.Web.Api.Worker.Models.Requests;

public sealed class ResizeImageMessageRequest
{
    public Guid? ImageId { get; init; }
    public int? TargetHeight { get; init; }
}