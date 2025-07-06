namespace ImageHub.Infrastructure.Settings;

public sealed class S3Settings
{
    public required string BucketName { get; init; }
    public int PreSignedUrlExpirationTimeInMinutes { get; init; }
}