using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageHub.Application.Helpers;

public static class EncoderHelper
{
    public static ImageEncoder GetEncoder(string mimeType)
    {
        return mimeType switch
        {
            "image/png" => new PngEncoder(),
            "image/jpeg" => new JpegEncoder(),
            "image/webp" => new SixLabors.ImageSharp.Formats.Webp.WebpEncoder(),
            _ => throw new NotSupportedException($"Unsupported mime type: {mimeType}")
        };
    }
}