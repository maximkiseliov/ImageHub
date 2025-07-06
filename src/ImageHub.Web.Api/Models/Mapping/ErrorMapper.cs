using FluentValidation.Results;
using ImageHub.Domain.Common;

namespace ImageHub.Web.Api.Models.Mapping;

public static class ErrorMapper
{
    public static ValidationError MapTo(this IEnumerable<ValidationFailure> validationFailures)
    {
        var errors = validationFailures
            .Select(f => Error.Validation(f.ErrorCode, f.ErrorMessage))
            .ToArray();

        return new ValidationError(errors);
    }
}