using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ImageHub.Application.Abstractions;
using ImageHub.Application.Models.Messages;
using ImageHub.Domain.Common;
using ImageHub.Domain.Errors;
using ImageHub.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageHub.Infrastructure.Services;

public sealed class SqsImageQueueService : IImageQueueService
{
    private readonly ILogger<SqsImageQueueService> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsSettings _sqsSettings;

    public SqsImageQueueService(
        ILogger<SqsImageQueueService> logger,
        IAmazonSQS sqsClient,
        IOptions<SqsSettings> sqsSettings)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _sqsSettings = sqsSettings.Value;
    }

    public async Task<Result> EnqueueResizeMessageAsync(ResizeImageMessage message, CancellationToken ct = default)
    {
        var sendRequest = new SendMessageRequest
        {
            MessageBody = JsonSerializer.Serialize(message),
            QueueUrl = _sqsSettings.ResizeQueueUrl
        };

        try
        {
            var response = await _sqsClient.SendMessageAsync(sendRequest, ct);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return Result.Success();
            }

            _logger.LogError("Failed to enqueue image '{ImageId} 'resize request. StatusCode: {StatusCode}", message.ImageId, response.HttpStatusCode);
            return Result.Failure(ImageErrors.MessageEnqueueFailed(message.ImageId));
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to enqueue image {ImageId} resize request", message.ImageId);
            return Result.Failure(ImageErrors.MessageEnqueueFailed(message.ImageId));
        }
    }
}