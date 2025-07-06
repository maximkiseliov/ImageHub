namespace ImageHub.Infrastructure.Settings;

public sealed class SqsSettings
{
    public required string ResizeQueueUrl { get; init; }
}