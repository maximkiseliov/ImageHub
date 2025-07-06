using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.SQS;
using ImageHub.Application.Abstractions;
using ImageHub.Infrastructure.Constants;
using ImageHub.Infrastructure.Repositories;
using ImageHub.Infrastructure.Services;
using ImageHub.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImageHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection
        AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddAwsServices(configuration)
            .AddRepositories()
            .AddServices();

    private static IServiceCollection AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.Configure<S3Settings>(configuration.GetSection(InfrastructureConstants.Config.Aws.S3.SectionName));
        services.Configure<SqsSettings>(configuration.GetSection(InfrastructureConstants.Config.Aws.Sqs.SectionName));

        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSQS>();

        services.AddSingleton<IDynamoDBContext>(sp =>
        {
            var client = sp.GetRequiredService<IAmazonDynamoDB>();

            return new DynamoDBContextBuilder()
                .WithDynamoDBClient(() => client)
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IImageRepository, DynamoDbImageRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();
        services.AddSingleton<IImageQueueService, SqsImageQueueService>();

        return services;
    }
}