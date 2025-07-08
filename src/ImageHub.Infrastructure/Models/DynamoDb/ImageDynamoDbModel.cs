using Amazon.DynamoDBv2.DataModel;

namespace ImageHub.Infrastructure.Models.DynamoDb;

[DynamoDBTable("Images")]
public sealed class ImageDynamoDbModel
{
    [DynamoDBHashKey]
    public required Guid Id { get; init; }
    public required string OriginalFileName { get; init; }
    public required string MimeType { get; init; }
    public long SizeInBytes { get; init; }
    public int OriginalHeight { get; init; }
    public DateTime CreatedAt { get; init; }
    public required Dictionary<string, string> Sizes { get; init; }
}