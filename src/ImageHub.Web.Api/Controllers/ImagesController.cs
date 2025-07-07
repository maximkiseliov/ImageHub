using FluentValidation;
using ImageHub.Application.Abstractions;
using ImageHub.Domain.Common;
using ImageHub.Domain.Extensions;
using ImageHub.Web.Api.Constants;
using ImageHub.Web.Api.Models.Mapping;
using ImageHub.Web.Api.Models.Requests;
using ImageHub.Web.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ImageHub.Web.Api.Controllers;

[ApiController]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly IValidator<UploadImageRequest> _uploadImageRequestValidator;
    private readonly IValidator<GetImageRequest> _getImageRequestValidator;
    private readonly IValidator<ResizeImageRequest> _resizeImageRequestValidator;

    public ImagesController(IImageService imageService,
        IValidator<UploadImageRequest> uploadImageRequestValidator,
        IValidator<GetImageRequest> getImageRequestValidator,
        IValidator<ResizeImageRequest> resizeImageRequestValidator)
    {
        _imageService = imageService;
        _uploadImageRequestValidator = uploadImageRequestValidator;
        _getImageRequestValidator = getImageRequestValidator;
        _resizeImageRequestValidator = resizeImageRequestValidator;
    }

    [HttpPost(WebApiConstants.ApiEndpoints.Images.Upload)]
    [ProducesResponseType<UploadImageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload([FromForm] UploadImageRequest apiRequest, CancellationToken ct)
    {
        var validationResult = await _uploadImageRequestValidator.ValidateAsync(apiRequest, ct);
        if (!validationResult.IsValid)
        {
            var result = Result.Failure(validationResult.Errors.MapTo());
            return GetResult(result);
        }

        var serviceRequest = apiRequest.Map();
        var serviceResult = await _imageService.ProcessImageAsync(serviceRequest, ct);

        return serviceResult.Match(
            onSuccess: guid => CreatedAtAction(nameof(Get), new { id = guid }, ImageMapper.Map(guid)),
            onFailure: GetResult
        );
    }

    [HttpGet(WebApiConstants.ApiEndpoints.Images.Get)]
    [ProducesResponseType<GetImageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromRoute] Guid id, [FromQuery] GetImageRequest apiRequest,
        CancellationToken ct)
    {
        var validationResult = await _getImageRequestValidator.ValidateAsync(apiRequest, ct);
        if (!validationResult.IsValid)
        {
            var result = Result.Failure(validationResult.Errors.MapTo());
            return GetResult(result);
        }

        var serviceRequest = apiRequest.Map(id);
        var serviceResult = await _imageService.GetImageAsync(serviceRequest, ct);

        return serviceResult.Match(
            onSuccess: r => Ok(ImageMapper.Map(r)),
            onFailure: GetResult);
    }

    [HttpPost(WebApiConstants.ApiEndpoints.Images.Resize)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resize([FromRoute] Guid id, [FromBody] ResizeImageRequest apiRequest,
        CancellationToken ct)
    {
        var validationResult = await _resizeImageRequestValidator.ValidateAsync(apiRequest, ct);
        if (!validationResult.IsValid)
        {
            var result = Result.Failure(validationResult.Errors.MapTo());
            return GetResult(result);
        }

        var serviceRequest = apiRequest.Map(id);
        var serviceResult = await _imageService.ResizeImageAsync(serviceRequest, ct);

        return serviceResult.Match(Accepted, GetResult);
    }

    [HttpDelete(WebApiConstants.ApiEndpoints.Images.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _imageService.DeleteImageAsync(id, ct);
        return result.Match(NoContent, GetResult);
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