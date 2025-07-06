using FluentValidation;
using ImageHub.Web.Api.Extensions;
using ImageHub.Web.Api.Models.Requests;

namespace ImageHub.Web.Api.Validators;

public sealed class ResizeImageRequestValidator : AbstractValidator<ResizeImageRequest>
{
    public ResizeImageRequestValidator()
    {
        RuleFor(x => x.Height)
            .Required()
            .ValidHeight();
    }
}