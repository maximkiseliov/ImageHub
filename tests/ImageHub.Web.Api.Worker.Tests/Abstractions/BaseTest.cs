using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;

namespace ImageHub.Web.Api.Worker.Tests.Abstractions;

public class BaseTest : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    
    protected BaseTest(TestWebAppFactory factory)
    {
        _factory = factory;
        HttpClient = factory.CreateClient();
    }

    protected HttpClient HttpClient { get; }
    protected IAmazonS3 S3Client => _factory.S3Client;
    protected IDynamoDBContext DynamoDbContext => _factory.DynamoDbContext;
}