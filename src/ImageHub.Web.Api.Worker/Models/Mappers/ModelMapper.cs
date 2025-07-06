using ImageHub.Application.Models.Messages;
using ImageHub.Web.Api.Worker.Models.Requests;

namespace ImageHub.Web.Api.Worker.Models.Mappers;

public static class ModelMapper
{
    public static MessageWrapper<ResizeImageMessage> Map(this ResizeImageMessageRequest request,
        IHeaderDictionary headers)
    {
        return new MessageWrapper<ResizeImageMessage>
        {
            Body = new ResizeImageMessage
            {
                ImageId = request.ImageId ?? Guid.Empty,
                TargetHeight = request.TargetHeight ?? 0
            },
            MessageId = headers.TryGetValue("X-Aws-Sqsd-Msgid", out var messageId) ? messageId.ToString() : string.Empty
        };
    }
}