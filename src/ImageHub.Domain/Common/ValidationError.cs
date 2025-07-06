namespace ImageHub.Domain.Common;

public sealed class ValidationError : Error
{
    public ValidationError(Error[] errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation)

    {
        Errors = errors;
    }

    public Error[] Errors { get; }
}