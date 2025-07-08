using ImageHub.Application.Helpers;
using Shouldly;

namespace ImageHub.Application.Tests.Helpers;

public class StorageKeyHelperTests
{
    [Fact]
    public void GetKey_ShouldReturnFormattedKey_WhenValidInputsProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string fileName = "image.png";
        const int height = 1080;

        // Act
        var key = StorageKeyHelper.GetKey(id, fileName, height);

        // Assert
        key.ShouldBe($"images/{id}/{height}/image.png");
    }

    [Fact]
    public void GetKey_ShouldReplaceInvalidCharacters_WhenFileNameContainsInvalidChars()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string fileName = "image@#$.png";
        const int height = 720;

        // Act
        var key = StorageKeyHelper.GetKey(id, fileName, height);

        // Assert
        key.ShouldBe($"images/{id}/{height}/image___.png");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetKey_ShouldReturnFormattedKey_WhenFileNameIsMissing(string fileName)
    {
        // Arrange
        var id = Guid.NewGuid();
        const int height = 480;

        // Act
        var key = StorageKeyHelper.GetKey(id, fileName, height);

        // Assert
        key.ShouldBe($"images/{id}/{height}/_");
    }
}