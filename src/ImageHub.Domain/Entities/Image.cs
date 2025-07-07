namespace ImageHub.Domain.Entities;

public sealed class Image
{
    private Image() { }
    
    public Guid Id { get; private init; }
    public string OriginalFileName { get; private init; } = null!;
    public string MimeType { get; private init; } = null!;
    public long SizeInBytes { get; private init; }
    public int OriginalHeight { get; private init; }
    public DateTime CreatedAt { get; private init; }
    private readonly Dictionary<string, string> _sizes = new();
    public IReadOnlyDictionary<string, string> Sizes => _sizes;

    public static Image Create(
        string fileName,
        string contentType,
        long sizeInBytes,
        int height)
    {
        return new Image
        {
            Id = Guid.NewGuid(),
            OriginalFileName = fileName,
            MimeType = contentType,
            SizeInBytes = sizeInBytes,
            OriginalHeight = height,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Image FromPersistence(
        Guid id,
        string fileName,
        string mimeType,
        long sizeInBytes,
        int height,
        DateTime createdAt,
        Dictionary<string, string> sizes)
    {
        var image = new Image
        {
            Id = id,
            OriginalFileName = fileName,
            MimeType = mimeType,
            SizeInBytes = sizeInBytes,
            OriginalHeight = height,
            CreatedAt = createdAt
        };

        foreach (var (key, value) in sizes)
        {
            image.AddSize(key, value);
        }

        return image;
    }

    public string? GetImagePath(int? requestedHeight)
    {
        return _sizes!.GetValueOrDefault(requestedHeight is null or <= 0
            ? OriginalHeight.ToString()
            : requestedHeight.ToString());
    }

    public void AddSize(string height, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _sizes[height] = path;
    }
}