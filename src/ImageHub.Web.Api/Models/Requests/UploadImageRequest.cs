namespace ImageHub.Web.Api.Models.Requests;

public sealed class UploadImageRequest
{
    public IFormFile? File { get; init; }
}