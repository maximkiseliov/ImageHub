using ImageHub.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ImageHub.Web.Api.Worker.Models.Mappers;

public static class ProblemDetailsMapper
{
    public static ProblemDetails Map(this Result result)
    {
        return result.Error.Type switch
        {
            ErrorType.Validation => new ValidationProblemDetails
            {
                Title = result.Error.Code,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Errors = result.Error is ValidationError validationError
                    ? validationError.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                    : new Dictionary<string, string[]>()
            },

            ErrorType.NotFound => new ProblemDetails
            {
                Title = result.Error.Code,
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Detail = result.Error.Description
            },

            _ => new ProblemDetails
            {
                Title = result.Error.Code,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6",
                Detail = result.Error.Description
            }
        };
    }
}