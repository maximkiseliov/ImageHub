using System.Net;
using System.Net.Http.Json;
using Amazon.S3.Model;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Web.Api.Models.Responses;
using ImageHub.Web.Api.Tests.Abstractions;
using NSubstitute;
using Shouldly;

namespace ImageHub.Web.Api.Tests.Images;

public class GetTests : BaseTest
{
    public GetTests(TestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_ShouldReturnOkResult_WhenSucceeds()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        const int targetHeight = 600;

        var dynamoDbModel = new ImageDynamoDbModel
        {
            Id = imageId,
            OriginalFileName = "test.jpg",
            MimeType = "image/jpeg",
            SizeInBytes = 1024,
            OriginalHeight = 1200,
            CreatedAt = DateTime.UtcNow,
            Sizes = new Dictionary<string, string>
            {
                { targetHeight.ToString(), $"images/{imageId}/{targetHeight}/test.jpg" }
            }
        };

        DynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>()).Returns(dynamoDbModel);

        const string presignedUrl = "https://s3-bucket.amazonaws.com/images/test.jpg?signature=xyz";
        S3Client.GetPreSignedURLAsync(Arg.Any<GetPreSignedUrlRequest>()).Returns(presignedUrl);

        // Act
        var response = await HttpClient.GetAsync($"/api/images/{imageId}?height={targetHeight}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GetImageResponse>();
        result.ShouldNotBeNull();
        result.Url.ShouldBe(presignedUrl);
    }
}