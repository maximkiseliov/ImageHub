using FluentValidation;
using ImageHub.Web.Api.Extensions;
using ImageHub.Web.Api.Models.Requests;

namespace ImageHub.Web.Api.Validators;

public sealed class GetImageRequestValidator : AbstractValidator<GetImageRequest>
{
    public GetImageRequestValidator()
    {
        RuleFor(x => x.Height)
            .ValidHeight()
            .When(x => x.Height.HasValue);
    }
}