using ImageHub.Application.Helpers;
using ImageHub.Application.Models.Messages;
using ImageHub.Application.Models.Storage;
using ImageHub.Domain.Entities;

namespace ImageHub.Application.Models.Mappers;

public static class ModelMapper
{
    public static ImageUploadRequest Map(this Image image, int height)
    {
        return new ImageUploadRequest
        {
            Key = StorageKeyHelper.GetKey(image.Id, image.OriginalFileName, height),
            ContentType = image.MimeType,
            FileName = image.OriginalFileName
        };
    }

    public static ResizeImageMessage Map(Guid id, int targetHeight)
    {
        return new ResizeImageMessage
        {
            ImageId = id,
            TargetHeight = targetHeight
        };
    }
}