using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace ImageHub.Web.Api.Worker.Tests.Abstractions;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IAmazonS3 S3Client { get; } = Substitute.For<IAmazonS3>();
    private IAmazonSQS SqsClient { get; } = Substitute.For<IAmazonSQS>();
    private IAmazonDynamoDB DynamoDbClient { get; } = Substitute.For<IAmazonDynamoDB>();
    public IDynamoDBContext DynamoDbContext { get; } = Substitute.For<IDynamoDBContext>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAmazonS3>();
            services.RemoveAll<IAmazonSQS>();
            services.RemoveAll<IAmazonDynamoDB>();
            services.RemoveAll<IDynamoDBContext>();

            services.AddSingleton(S3Client);
            services.AddSingleton(SqsClient);
            services.AddSingleton(DynamoDbClient);
            services.AddSingleton(DynamoDbContext);

            services.RemoveAll<Serilog.ILogger>();
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new Task DisposeAsync() => Task.CompletedTask;
}