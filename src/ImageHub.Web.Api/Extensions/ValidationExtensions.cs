using FluentValidation;

namespace ImageHub.Web.Api.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, int?> Required<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode("Required")
            .WithMessage("{PropertyName} is required");
    }

    public static IRuleBuilderOptions<T, IFormFile?> Required<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder)
    {
        return ruleBuilder
            .Must(f => f is { Length: > 0 })
            .WithErrorCode("Required")
            .WithMessage("{PropertyName} is required");
    }
    
    public static IRuleBuilderOptions<T, IFormFile?> ValidImageFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder)
    {
        return ruleBuilder
            .Must(f => f?.Length <= Constants.WebApiConstants.MaxFileSize)
            .WithErrorCode("InvalidFileSize")
            .WithMessage($"Size must not exceed {Constants.WebApiConstants.MaxFileSize} MB")
            .Must(f => f?.ContentType.StartsWith("image/") == true)
            .WithErrorCode("InvalidContentType")
            .WithMessage("File must be an image.");
    }
    
    public static IRuleBuilderOptions<T, int?> ValidHeight<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithErrorCode("InvalidHeight")
            .WithMessage("Height must be greater than 0.");
    }
}