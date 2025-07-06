using ImageHub.Web.Api.Models.Requests;
using ImageHub.Web.Api.Models.Responses;

namespace ImageHub.Web.Api.Models.Mapping;

public static class ImageMapper
{
    public static UploadImageResponse Map(Guid id) => new() { Id = id };

    public static GetImageResponse Map(string presignedUrl) => new() { Url = presignedUrl };
    
    public static Application.Models.Requests.UploadImageRequest Map(this UploadImageRequest request)
    {
        return new Application.Models.Requests.UploadImageRequest
        {
            Content = request.File!.OpenReadStream(),
            ContentType = request.File.ContentType,
            FileName = request.File.FileName
        };
    }
    
    public static Application.Models.Requests.GetImageRequest Map(this GetImageRequest request, Guid id)
    {
        return new Application.Models.Requests.GetImageRequest
        {
            Id = id,
            Height = request.Height ?? 0
        };
    }

    public static Application.Models.Requests.ResizeImageRequest Map(this ResizeImageRequest apiRequest, Guid id)
    {
        return new Application.Models.Requests.ResizeImageRequest
        {
            Id = id,
            Height = apiRequest.Height ?? 0
        };
    }
}