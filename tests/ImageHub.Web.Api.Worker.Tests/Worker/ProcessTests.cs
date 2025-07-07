using System.Net;
using System.Net.Http.Json;
using Amazon.S3.Model;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Web.Api.Worker.Models.Requests;
using ImageHub.Web.Api.Worker.Tests.Abstractions;
using NSubstitute;
using Shouldly;

namespace ImageHub.Web.Api.Worker.Tests.Worker;

public class ProcessTests : BaseTest
{
    public ProcessTests(TestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Process_WhenSucceeds_ReturnsOkResult()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var request = new ResizeImageMessageRequest
        {
            ImageId = imageId,
            TargetHeight = 600
        };

        var dynamoDbModel = new ImageDynamoDbModel
        {
            Id = imageId,
            OriginalFileName = "test.jpg",
            MimeType = "image/jpeg",
            SizeInBytes = 1024,
            OriginalHeight = 1200,
            CreatedAt = DateTime.UtcNow,
            Sizes = new Dictionary<string, string>()
            {
                { "1200", $"images/{imageId}/1200/test.jpg" }
            }
        };
        DynamoDbContext.LoadAsync<ImageDynamoDbModel>(imageId, Arg.Any<CancellationToken>()).Returns(dynamoDbModel);

        var sampleImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "sample.jpg");
        var sampleImageBytes = await File.ReadAllBytesAsync(sampleImagePath);
        using var memorySteam = new MemoryStream(sampleImageBytes);
        S3Client.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>()).Returns(new GetObjectResponse
            { HttpStatusCode = HttpStatusCode.OK, ResponseStream = memorySteam });

        S3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        DynamoDbContext.SaveAsync(Arg.Any<ImageDynamoDbModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/process", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}