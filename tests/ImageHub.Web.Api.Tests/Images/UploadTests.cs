using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.S3.Model;
using Amazon.SQS.Model;
using ImageHub.Infrastructure.Models.DynamoDb;
using ImageHub.Web.Api.Models.Responses;
using ImageHub.Web.Api.Tests.Abstractions;
using NSubstitute;
using Shouldly;

namespace ImageHub.Web.Api.Tests.Images;

public class UploadTests : BaseTest
{
    public UploadTests(TestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Upload_WhenSucceeds_ReturnsCreatedResult()
    {
        // Arrange
        S3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        DynamoDbContext.SaveAsync(Arg.Any<ImageDynamoDbModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        SqsClient.SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var sampleImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "sample.jpg");
        var sampleImageBytes = await File.ReadAllBytesAsync(sampleImagePath);

        var multipartContent = new MultipartFormDataContent
        {
            {
                new ByteArrayContent(sampleImageBytes)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("image/jpeg")
                    }
                },
                "File",
                "test.jpg"
            }
        };

        // Act
        var response = await HttpClient.PostAsync("/api/images", multipartContent);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain("/api/images/");

        var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>();
        result.ShouldNotBeNull();

        await S3Client.Received().PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());

        await DynamoDbContext.Received()
            .SaveAsync(
                Arg.Is<ImageDynamoDbModel>(model =>
                    model.OriginalFileName == "test.jpg" && model.MimeType == "image/jpeg"),
                Arg.Any<CancellationToken>());

        await SqsClient.Received().SendMessageAsync(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>());
    }
}