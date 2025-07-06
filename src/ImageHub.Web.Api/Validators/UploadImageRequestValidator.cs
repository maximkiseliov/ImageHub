using FluentValidation;
using ImageHub.Web.Api.Extensions;
using ImageHub.Web.Api.Models.Requests;

namespace ImageHub.Web.Api.Validators;

public sealed class UploadImageRequestValidator : AbstractValidator<UploadImageRequest>
{
    public UploadImageRequestValidator()
    {
        RuleFor(x => x.File)
            .Required();
        
        RuleFor(x => x.File)
            .ValidImageFile()
            .When(x => x.File is not null);
    }
}