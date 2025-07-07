using Amazon.SQS;
using Amazon.SQS.Model;
using ImageHub.Application.Models.Messages;
using ImageHub.Infrastructure.Services;
using ImageHub.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace ImageHub.Infrastructure.Tests.Services;

public class SqsImageQueueServiceTests
{
    private readonly SqsImageQueueService _sut;
    private readonly FakeLogger<SqsImageQueueService> _logger;
    private readonly IAmazonSQS _amazonSqs;

    public SqsImageQueueServiceTests()
    {
        _logger = new FakeLogger<SqsImageQueueService>();
        _amazonSqs = Substitute.For<IAmazonSQS>();

        var sqsSettings = Substitute.For<IOptions<SqsSettings>>();
        var settings = new SqsSettings { ResizeQueueUrl = "sqs-url" };
        sqsSettings.Value.Returns(settings);

        _sut = new SqsImageQueueService(_logger, _amazonSqs, sqsSettings);
    }

    [Fact]
    public async Task EnqueueResizeMessageAsync_ShouldReturnSuccess_WhenSqsSendSucceeds()
    {
        // Arrange
        var message = new ResizeImageMessage
        {
            ImageId = Guid.NewGuid(),
            TargetHeight = 600
        };

        _amazonSqs.SendMessageAsync(
                Arg.Is<SendMessageRequest>(r =>
                    r.QueueUrl == "sqs-url" &&
                    r.MessageBody.Contains(message.ImageId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(new SendMessageResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                MessageId = "test-message-id"
            });

        // Act
        var result = await _sut.EnqueueResizeMessageAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var logMessages = _logger.Collector.GetSnapshot();
        logMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task EnqueueResizeMessageAsync_ShouldReturnFailure_WhenSqsReturnsNonOkStatus()
    {
        // Arrange
        var message = new ResizeImageMessage
        {
            ImageId = Guid.NewGuid(),
            TargetHeight = 600
        };

        _amazonSqs.SendMessageAsync(
                Arg.Any<SendMessageRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SendMessageResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.InternalServerError
            });

        // Act
        var result = await _sut.EnqueueResizeMessageAsync(message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Description.ShouldContain($"Message with image Id '{message.ImageId}' queue enqueue failed");

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to enqueue image '{message.ImageId} 'resize request");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task EnqueueResizeMessageAsync_ShouldReturnFailure_WhenSqsThrowsException()
    {
        // Arrange
        var message = new ResizeImageMessage
        {
            ImageId = Guid.NewGuid(),
            TargetHeight = 600
        };

        _amazonSqs.SendMessageAsync(
                Arg.Any<SendMessageRequest>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonSQSException("Queue send failed"));

        // Act
        var result = await _sut.EnqueueResizeMessageAsync(message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Description.ShouldContain($"Message with image Id '{message.ImageId}' queue enqueue failed");

        var logMessages = _logger.Collector.GetSnapshot();
        logMessages[0].Message.ShouldContain($"Failed to enqueue image {message.ImageId} resize request");
        logMessages[0].Level.ShouldBe(LogLevel.Error);
    }
}