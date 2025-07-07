using ImageHub.Application.Helpers;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using Shouldly;

namespace ImageHub.Application.Tests.Helpers;

public class EncoderHelperTests
{
    [Fact]
    public void GetEncoder_ShouldReturnPngEncoder_WhenPngMimeTypeProvided()
    {
        // Arrange
        // Act
        var encoder = EncoderHelper.GetEncoder("image/png");
        
        // Assert
        encoder.ShouldBeOfType<PngEncoder>();
    }

    [Fact]
    public void GetEncoder_ShouldReturnJpegEncoder_WhenJpegMimeTypeProvided()
    {
        // Arrange
        // Act
        var encoder = EncoderHelper.GetEncoder("image/jpeg");
        
        // Assert
        encoder.ShouldBeOfType<JpegEncoder>();
    }

    [Fact]
    public void GetEncoder_ShouldReturnWebpEncoder_WhenWebpMimeTypeProvided()
    {
        // Arrange
        // Act
        var encoder = EncoderHelper.GetEncoder("image/webp");
        
        // Assert
        encoder.ShouldBeOfType<SixLabors.ImageSharp.Formats.Webp.WebpEncoder>();
    }

    [Fact]
    public void GetEncoder_ShouldThrowNotSupportedException_WhenUnsupportedMimeTypeProvided()
    {
        // Arrange
        // Act
        // Assert
        Should.Throw<NotSupportedException>(() => EncoderHelper.GetEncoder("image/gif"))
            .Message.ShouldBe("Unsupported mime type: image/gif");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetEncoder_ShouldThrowNotSupportedException_WhenMissingMimeTypeProvided(string mimeType)
    {
        // Arrange
        // Act
        // Assert
        Should.Throw<NotSupportedException>(() => EncoderHelper.GetEncoder(mimeType))
            .Message.ShouldBe("Unsupported mime type: ");
    }
}