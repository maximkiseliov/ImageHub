using System.Net;
using System.Net.Http.Json;
using Amazon.SQS.Model;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Web.Api.Models.Requests;
using ImageHub.Web.Api.Tests.Abstractions;
using NSubstitute;
using Shouldly;

namespace ImageHub.Web.Api.Tests.Images;

public class ResizeTests : BaseTest
{
    public ResizeTests(TestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Resize_WhenSucceeds_ReturnsAcceptedResult()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        const int targetHeight = 600;
        var request = new ResizeImageRequest
        {
            Height = targetHeight
        };

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
                { "1200", $"images/{imageId}/1200/test.jpg" }
            }
        };

        DynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>())
            .Returns(imageDynamoDbModel);

        SqsClient.SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        var response = await HttpClient.PostAsJsonAsync($"/api/images/{imageId}/resize", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await DynamoDbContext.Received().LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>());

        await SqsClient.Received()
            .SendMessageAsync(
                Arg.Is<SendMessageRequest>(req =>
                    req.MessageBody.Contains(imageId.ToString()) && req.MessageBody.Contains(targetHeight.ToString())),
                Arg.Any<CancellationToken>());
    }
}