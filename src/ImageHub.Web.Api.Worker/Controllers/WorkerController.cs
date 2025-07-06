using ImageHub.Application.Abstractions;
using ImageHub.Domain.Common;
using ImageHub.Domain.Extensions;
using ImageHub.Web.Api.Worker.Models.Mappers;
using ImageHub.Web.Api.Worker.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace ImageHub.Web.Api.Worker.Controllers;

[ApiController]
public sealed class WorkerController : ControllerBase
{
    private readonly IImageService _imageService;

    public WorkerController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("/process")]
    public async Task<IActionResult> Process([FromBody] ResizeImageMessageRequest apiRequest)
    {
        var serviceRequest = apiRequest.Map(Request.Headers);
        var serviceResult = await _imageService.ProcessImageResizeAsync(serviceRequest, CancellationToken.None);

        return serviceResult.Match(
            onSuccess: Ok,
            onFailure: GetResult);
    }

    private IActionResult GetResult(Result result)
    {
        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(result.Map()),
            ErrorType.NotFound => NotFound(result.Map()),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Map())
        };
    }
}