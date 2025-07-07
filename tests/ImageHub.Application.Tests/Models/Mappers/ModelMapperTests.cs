using ImageHub.Application.Models.Mappers;
using ImageHub.Domain.Entities;
using Shouldly;

namespace ImageHub.Application.Tests.Models.Mappers;

public class ModelMapperTests
{
    [Fact]
    public void Map_ShouldReturnImageUploadRequest_WithCorrectProperties_WhenMappingImage()
    {
        // Arrange
        var image = Image.Create(
            "sample.jpg", 
            "image/jpeg", 
            2048, 
            1200);
        const int height = 800;

        // Act
        var result = image.Map(height);

        // Assert
        result.Key.ShouldBe($"images/{image.Id}/{height}/sample.jpg");
        result.ContentType.ShouldBe("image/jpeg");
        result.FileName.ShouldBe("sample.jpg");
    }

    [Fact]
    public void Map_ShouldReturnResizeImageMessage_WithCorrectProperties_WhenMappingGuidAndHeight()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        const int targetHeight = 600;

        // Act
        var result = ModelMapper.Map(imageId, targetHeight);

        // Assert
        result.ImageId.ShouldBe(imageId);
        result.TargetHeight.ShouldBe(targetHeight);
    }
}