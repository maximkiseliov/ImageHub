using System.Net;
using Amazon.S3.Model;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Web.Api.Tests.Abstractions;
using NSubstitute;
using Shouldly;

namespace ImageHub.Web.Api.Tests.Images;

public class DeleteTests : BaseTest
{
    public DeleteTests(TestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Delete_ShouldReturnsNoContentResult_WhenSucceeds()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        var imageDynamoDbModel = new ImageDynamoDbModel
        {
            Id = imageId,
            OriginalFileName = "test.jpg",
            MimeType = "image/jpeg",
            SizeInBytes = 1024,
            OriginalHeight = 1200,
            CreatedAt = DateTime.UtcNow,
            Sizes = new Dictionary<string, string>
            {
                { "1200", $"images/{imageId}/1200/test.jpg" },
                { "600", $"images/{imageId}/600/test.jpg" }
            }
        };

        DynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Returns(imageDynamoDbModel);

        S3Client.ListObjectsV2Async(Arg.Any<ListObjectsV2Request>(), Arg.Any<CancellationToken>())
            .Returns(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.OK,
                S3Objects =
                [
                    new S3Object { Key = $"images/{imageId}/1200/test.jpg" },
                    new S3Object { Key = $"images/{imageId}/600/test.jpg" }
                ]
            });

        S3Client.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        DynamoDbContext.DeleteAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var response = await HttpClient.DeleteAsync($"/api/images/{imageId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await DynamoDbContext.Received().LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>());

        await S3Client.Received().ListObjectsV2Async(
            Arg.Is<ListObjectsV2Request>(req =>
                req.Prefix == $"images/{imageId}/"),
            Arg.Any<CancellationToken>());

        await S3Client.Received(2).DeleteObjectAsync(
            Arg.Any<DeleteObjectRequest>(),
            Arg.Any<CancellationToken>());

        await DynamoDbContext.Received().DeleteAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>());
    }
}